using System.Buffers;
using System.Net.Sockets;
using System.Text;
using Genesis.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Network;

public sealed class Client : IDisposable
{
    private const char CARRIAGE_RETURN = '\r';
    private static readonly int CLOSE_TIMEOUT = 100;

    private readonly Timer _keepAlive;
    private readonly ILogger<Client> _logger;
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly Socket _socket;

    internal Client(ILogger<Client> logger, Socket socket)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));

        _socket.NoDelay = true;
        _keepAlive = new(KeepAlive, null, 0, 30000);
        Address = _socket.RemoteEndPoint?.ToString() ?? "Unknown";
    }

    public string Address { get; }

    public Player? Player { get; private set; }

    public Guid Id { get; } = Guid.NewGuid();

    public string Procedure { get; set; } = "Login";

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    public void Disconnect(Driver driver, string message = "Disconnected")
    {
        try
        {
            if (Player is not null)
            {
                driver.Runtime.DoProcedure("Logout", "DoLogout", driver, Player);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Disconnect failed unexpectedly");
        }
        finally
        {
            _socket.Shutdown(SocketShutdown.Both);

            driver.Network.Unregister(this);

            _socket.Close(CLOSE_TIMEOUT);

            _keepAlive.Dispose();

            Player = null;
        }
    }

    public void Dispose()
    {
        _socket.Dispose();
    }

    public void SendBytes(byte[] bytes)
    {
        if (_socket.Connected == true)
        {
            _socket.SendAsync(bytes).FireAndForget(ex =>
            {
                _logger.LogWarning(ex, "SendBytes failed unexpectedly");
            });
        }
    }

    public void SetPlayer(Driver driver, Player player)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(player);

        Player?.Client?.Disconnect(driver);
        Procedure = string.Empty;
        player.Client = this;
        Player = player;
    }

    internal void Start(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        try
        {
            driver.Network.Register(this);

            ReceiveAsync(driver).FireAndForget(ex =>
            {
                Disconnect(driver, "ReceiveAsync failed unexpectedly");
            });

            driver.Runtime.DoProcedure(Procedure, $"Do{Procedure}", driver, this);
        }
        catch (Exception ex)
        {
            Disconnect(driver, "Disconnected");

            _logger.LogWarning(ex, "Start failed unexpectedly");
        }
    }

    private void KeepAlive(object? state = null)
    {
        SendBytes([0x0]);
    }

    private void ProcessMessage(Driver driver, string message)
    {
        ArgumentNullException.ThrowIfNull(driver);

        try
        {
            if (string.IsNullOrWhiteSpace(message) == false)
            {
                if (string.IsNullOrWhiteSpace(Procedure) == true)
                {
                    if (Player is not null)
                    {
                        driver.Runtime.DoAction(driver, Player, message.Trim());
                    }
                }
                else
                {
                    driver.Runtime.DoProcedure(Procedure, $"Do{Procedure}", driver, this, message.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProcessMessage Failed Unexpectedly");
        }
        finally
        {
            if (_socket.Connected == true)
            {
                ReceiveAsync(driver).FireAndForget(ex =>
                {
                    Disconnect(driver, "Connection failed unexpectedly");
                    _logger.LogWarning(ex, "ProcessMessage failed unexpectedly");
                });
            }
        }
    }

    private async Task ReceiveAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        var bytes = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            var buffer = new Memory<byte>(bytes, 0, bytes.Length);
            
            var count = await _socket.ReceiveAsync(buffer, SocketFlags.None);

            if (count == 0)
            {
                Disconnect(driver, "Disconnected");
            }
            else
            {
                var message = Encoding.UTF8.GetString(bytes, 0, count).Split(CARRIAGE_RETURN);

                ProcessMessage(driver, string.Join(' ', message[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }
}
