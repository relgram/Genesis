using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private static readonly JsonSerializerOptions OPTIONS = new() { WriteIndented = true };

    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Entity>> _entities = [];
    private readonly ILogger<Manager> _logger;
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[100];

    public Manager(ILogger<Manager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ContentPath = configuration["Genesis:ContentPath"] ?? throw new ArgumentException("ContentPath not defined");

        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(x => x.IsAbstract is false))
        {
            if (type.IsAssignableTo(typeof(Entity)) is true)
            {
                _entities[type] = [];
            }
        }
    }

    public string ContentPath { get; }

    public T[] Find<T>(Func<T, bool> predicate) where T : Entity
    {
        return [.. _entities[typeof(T)].Values.Cast<T>().Where(predicate)];
    }

    public T? First<T>(Func<T, bool> predicate) where T : Entity
    {
        return _entities[typeof(T)].Values.Cast<T>().Where(predicate).FirstOrDefault();
    }

    public T? Get<T>(Guid entityId) where T : Entity
    {
        return _entities[typeof(T)].GetValueOrDefault(entityId) as T;
    }

    public void Save(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (_logger.IsEnabled(LogLevel.Information) == true)
        {
            _logger.LogInformation("Saving Player: {Name}", player.Name);
        }

        var directory = Path.Join(ContentPath, "Players");
        var dest = Path.Join(directory, $"{player.Name}.json");
        var temp = Path.Join(directory, $"{player.Name}.temp.json");
        var contents = JsonSerializer.Serialize(player, OPTIONS);

        if (File.Exists(dest) == false)
        {
            File.WriteAllText(dest, contents);
        }
        else
        {
            File.WriteAllText(temp, contents);
            File.Replace(temp, dest, destinationBackupFileName: null);
        }
    }

    public void Save(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (_logger.IsEnabled(LogLevel.Information) == true)
        {
            _logger.LogInformation("Saving Region: {Name}", region.Name);
        }

        var directory = Path.Join(ContentPath, "Regions");
        var dest = Path.Join(directory, $"{region.Id}.json");
        var temp = Path.Join(directory, $"{region.Id}.temp.json");
        var contents = JsonSerializer.Serialize(region, OPTIONS);

        if (File.Exists(dest) == false)
        {
            File.WriteAllText(dest, contents);
        }
        else
        {
            File.WriteAllText(temp, contents);
            File.Replace(temp, dest, destinationBackupFileName: null);
        }
    }

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_logger.IsEnabled(LogLevel.Information) == true)
        {
            _logger.LogInformation("Register Entity: {Name}", entity.Name);
        }

        if (_entities[entity.GetType()].TryAdd(entity.Id, entity) == true)
        {
            entity.IsRegistered = true;
            _updateTimers[(uint)entity.Id.GetHashCode() % _updateTimers.Length].Register(entity);
        }
    }

    internal void Start(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(x => _updateTimers[x] = new(driver));

        var root = Path.Join(ContentPath, "Regions");

        foreach (string path in Directory.GetFiles(root, "*.json"))
        {
            JsonSerializer.Deserialize<Region>(File.ReadAllText(path))?.Load(driver);
        }

        _logger.LogInformation("Content Manager Started");
    }

    internal void Stop(Driver driver, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(driver);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(x => _updateTimers[x].Dispose());

        Find<Region>(region => true).ForEach(region => region.Unload(driver));

        _logger.LogInformation("Content Manager Stopped");
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_logger.IsEnabled(LogLevel.Information) == true)
        {
            _logger.LogInformation("Unregister Entity: {Name}", entity.Name);
        }

        if (_entities[entity.GetType()].TryRemove(entity.Id, out _) == true)
        {
            entity.IsRegistered = false;
            _updateTimers[(uint)entity.Id.GetHashCode() % _updateTimers.Length].Unregister(entity);
        }
    }
}
