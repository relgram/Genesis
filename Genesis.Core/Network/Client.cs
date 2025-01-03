using System.Net.Sockets;
using System.Text;
using Genesis.Core.Content;
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

    public Entity? Entity { get; private set; }

    public Guid Id { get; } = Guid.NewGuid();

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

        try
        {
            if (string.IsNullOrWhiteSpace(message) == false)
            {
                if (string.IsNullOrWhiteSpace(Procedure) == true)
                {
                    if (Entity is not null)
                    {
                        engine.Runtime.DoAction(engine, Entity, message.Trim());
                    }
                }
                else
                {
                    engine.Runtime.DoProcedure(engine, Procedure, $"Do{Procedure}", this, message.Trim());
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
                ReceiveAsync(engine).FireAndForget(ex =>
                {
                    Disconnect(engine, "Connection failed unexpectedly");
                    _logger.LogWarning(ex, "ProcessMessage failed unexpectedly");
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
            if (Entity is Player player)
            {
                engine.Runtime.DoProcedure(engine, "Logout", "DoLogout", player);
            }

            message = $"<color red>{message}</color>";

            Entity?.Parent?.Unregister(Entity);

            SendBytes(message.ToBytes());

            Entity?.Unload(engine);
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

            Entity = null;
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

    public void SetEntity(GameEngine engine, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(entity);

        Procedure = string.Empty;
        entity.Client = this;
        Entity = entity;
    }
}
