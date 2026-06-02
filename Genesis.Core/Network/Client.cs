using System.Net.Sockets;
using System.Text;
using Genesis.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Network;

public sealed class Client : IDisposable
{
    private static readonly int TIMEOUT = 100;

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

    public Player Player { get; private set; } = new("");

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
                driver.Runtime.DoProcedure(Player, "Logout", "DoLogout", message);
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

            _socket.Close(TIMEOUT);
        }
    }

    public void Dispose()
    {
        _keepAlive?.Dispose();
        _socket?.Dispose();
    }

    public void SendBytes(byte[] bytes)
    {
        if (_socket.Connected == true)
        {
            _socket.SendAsync(bytes).FireAndForget(ex =>
            {
                if (_socket.Connected == true)
                {
                    _logger.LogWarning(ex, "SendBytes failed unexpectedly");
                }
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
            Player = new("")
            {
                Client = this
            };

            driver.Network.Register(this);

            ReceiveAsync(driver).FireAndForget(ex =>
            {
                Disconnect(driver, "ReceiveAsync failed unexpectedly");
            });

            driver.Runtime.DoProcedure(Player, Procedure, $"Do{Procedure}");
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
                    if (message[0] == '\'')
                    {
                        message = $"say {message[1..]}";
                    }

                    driver.Runtime.DoAction(Player, message);
                }
                else
                {
                    driver.Runtime.DoProcedure(Player, Procedure, $"Do{Procedure}", message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProcessMessage Failed Unexpectedly");
        }
    }

    private async Task ReceiveAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        byte[] buffer = new byte[128];

        StringBuilder messageBuilder = new();

        while (_socket.Connected == true)
        {
            try
            {
                var count = await _socket.ReceiveAsync(buffer);

                if (count == 0)
                {
                    Disconnect(driver, "Disconnected");
                }
                else
                {
                    string chunk = Encoding.UTF8.GetString(buffer, 0, count);

                    int terminatorIndex = chunk.IndexOf('\r');

                    if (terminatorIndex < 0)
                    {
                        messageBuilder.Append(chunk.AsSpan());
                    }
                    else
                    {
                        messageBuilder.Append(chunk.AsSpan(0, terminatorIndex)).SingleSpace();

                        ProcessMessage(driver, messageBuilder.ToString());

                        messageBuilder.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                messageBuilder.Clear();

                _logger.LogWarning(ex, "ReceiveAsync Failed Unexpectedly");
            }
        }
    }
}
