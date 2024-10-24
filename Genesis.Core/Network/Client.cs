using System.Net.Sockets;
using System.Text;
using Genesis.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Network;

public sealed class Client
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

    public Guid ClientId { get; } = Guid.NewGuid();

    public Player? Player { get; private set; }

    public string Procedure { get; set; } = "Login";

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    private void KeepAlive(object? state = null)
    {
        SendBytes([0x0]);
    }

    private void ProcessMessage(GameEngine engine, string message)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        try
        {
            if (string.IsNullOrWhiteSpace(Procedure) is true)
            {
                //if (Player is not null)
                //{
                //    engine.Runtime.DoAction(engine, Player, message.Trim());
                //}
            }
            else
            {
                engine.Runtime.DoProcedure(engine, Procedure, $"Do{Procedure}", this, message);
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
                ReceiveAsync(engine).FireAndForget(ex =>
                {
                    Disconnect(engine, "Connection failed unexpectedly");
                });
            }
        }
    }

    private async Task ReceiveAsync(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var bytes = new byte[4096];

        var count = await _socket.ReceiveAsync(bytes);

        if (count == 0)
        {
            Disconnect(engine, "Disconnected");
        }
        else
        {
            var message = Encoding.UTF8.GetString(bytes).Split(CARRIAGE_RETURN);

            ProcessMessage(engine, string.Join(' ', message[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        }
    }

    internal void Start(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        try
        {
            engine.Network.Register(this);

            ReceiveAsync(engine).FireAndForget(ex =>
            {
                Disconnect(engine, "ReceiveAsync failed unexpectedly");
            });

            engine.Runtime.DoProcedure(engine, Procedure, $"Do{Procedure}", this);
        }
        catch (Exception ex)
        {
            Disconnect(engine, "Disconnected");

            _logger.LogWarning(ex, "Start failed unexpectedly");
        }
    }

    public void Disconnect(GameEngine engine, string message = "Disconnected")
    {
        try
        {
            SendBytes(Encoding.UTF8.GetBytes($"*** {message} ***"));

            Player?.Save(engine, cascade: true);

            Player?.Unload(engine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Disconnect failed unexpectedly");
        }
        finally
        {
            _socket.Shutdown(SocketShutdown.Both);

            engine.Network.Unregister(this);

            _socket.Close(CLOSE_TIMEOUT);

            _keepAlive.Dispose();

            Player = null;
        }
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

    public void SetPlayer(GameEngine engine, Player player)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(player);

        Player?.Disconnect(engine);
        player.Client = this;
        Player = player;
    }
}
