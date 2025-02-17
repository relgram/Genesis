using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Network;

public sealed class Manager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private readonly ILogger<Manager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TcpListener _tcpListener = new(IPAddress.Any, 4000);

    public Manager(ILogger<Manager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public void Dispose()
    {
        _tcpListener.Dispose();
    }

    internal void Register(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _logger.LogInformation("Register client: {Address}", client.Address);

        if (_clients.TryAdd(client.Id, client) == false)
        {
            throw new ArgumentException($"Failed to register client: {client.Address}");
        }
    }

    internal void Start(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Network Manager...");

        _tcpListener.Start();

        AcceptSocketAsync(driver).FireAndForget(ex =>
        {
            _logger.LogCritical(ex, "AcceptSocketAsync Failed Unexpectedly");
        });

        _logger.LogInformation("Network Manager Started");
    }

    internal void Stop(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Network Manager...");

        _tcpListener.Stop();

        foreach (var client in _clients.Values)
        {
            client.Disconnect(driver, "Server Shutting Down");
        }

        _logger.LogInformation("Network Manager Stopped");
    }

    internal void Unregister(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _logger.LogInformation("Unregister client: {Address}", client.Address);

        if (_clients.TryRemove(client.Id, out _) == false)
        {
            throw new ArgumentException($"Failed to unregister client: {client.Address}");
        }
    }

    private async Task AcceptSocketAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        try
        {
            ILogger<Client> logger = _loggerFactory.CreateLogger<Client>();

            var socket = await _tcpListener.AcceptSocketAsync();

            Client client = new(logger, socket);

            client.Start(driver);
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
                AcceptSocketAsync(driver).FireAndForget(ex =>
                {
                    _logger.LogCritical(ex, "AcceptSocketAsync Failed Unexpectedly");
                });
            }
        }
    }
}
