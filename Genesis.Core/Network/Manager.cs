using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Network;

public sealed class Manager
{
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private readonly ILogger<Manager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TcpListener _tcpListener = new(IPAddress.Any, 4000);

    public Manager(ILogger<Manager> logger, ILoggerFactory loggerFmobiley)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFmobiley ?? throw new ArgumentNullException(nameof(loggerFmobiley));
    }

    private async Task AcceptSocketAsync(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        try
        {
            ILogger<Client> logger = _loggerFactory.CreateLogger<Client>();

            var socket = await _tcpListener.AcceptSocketAsync();

            Client client = new(logger, socket);

            client.Start(engine);
        }
        catch (Exception ex)
        {
            if (ex is not SocketException)
            {
                _logger.LogCritical(ex, "AcceptSocketAsync Failed Unexpectedly");
            }
        }
        finally
        {
            if (_tcpListener.Server.IsBound)
            {
                AcceptSocketAsync(engine).FireAndForget(ex =>
                {
                    _logger.LogCritical(ex, "AcceptSocketAsync Failed Unexpectedly");
                });
            }
        }
    }

    internal void Register(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _logger.LogInformation("Register client: {address}", client.Address);

        if (_clients.TryAdd(client.Id, client) == false)
        {
            throw new ArgumentException($"Failed to register client: {client.Address}");
        }
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Network Manager...");

        _tcpListener.Start();

        AcceptSocketAsync(engine).FireAndForget(ex =>
        {
            _logger.LogCritical(ex, "AcceptSocketAsync Failed Unexpectedly");
        });

        _logger.LogInformation("Network Manager Started");
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Network Manager...");

        _tcpListener.Stop();

        foreach (var client in _clients.Values)
        {
            client.Disconnect(engine, "Server Shutting Down");
        }

        _logger.LogInformation("Network Manager Stopped");
    }

    internal void Unregister(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _logger.LogInformation("Unregister client: {address}", client.Address);

        if (_clients.TryRemove(client.Id, out _) == false)
        {
            throw new ArgumentException($"Failed to unregister client: {client.Address}");
        }
    }
}
