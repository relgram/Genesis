using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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

    /// <summary>
    /// Returns array of all registered entities of type T matching provided predicate.
    /// </summary>
    public T[] Find<T>(Func<T, bool> predicate) where T : Entity
    {
        return [.. _entities[typeof(T)].Values.Cast<T>().Where(predicate)];
    }

    /// <summary>
    /// Returns the first registered entity of type T matching provided predicate. 
    /// </summary>
    public T? First<T>(Func<T, bool> predicate) where T : Entity
    {
        return _entities[typeof(T)].Values.Cast<T>().Where(predicate).FirstOrDefault();
    }

    /// <summary>
    /// Returns registered entity of type T with specified entityId.
    /// </summary>
    public T? Get<T>(Guid entityId) where T : Entity
    {
        return _entities[typeof(T)].GetValueOrDefault(entityId) as T;
    }

    /// <summary>
    /// Save provided Player to file system.
    /// </summary>
    public void Save(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        //_logger.LogInformation("Saving Player: {Id}", player.Id);

        var directory = Path.Join(ContentPath, "Players");
        var target = Path.Join(directory, $"{player.Name}.json");
        var temp = Path.Join(directory, $"{player.Name}.tmp.json");

        var contents = JsonSerializer.Serialize(player, OPTIONS);

        File.WriteAllText(temp, contents);
        File.Replace(temp, target, null);
    }

    /// <summary>
    /// Save provided Region to file system.
    /// </summary>
    public void Save(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        //_logger.LogInformation("Saving Region: {Id}", region.Id);

        var directory = Path.Join(ContentPath, "Regions");
        var target = Path.Join(directory, $"{region.Id}.json");
        var temp = Path.Join(directory, $"{region.Id}.tmp.json");

        var contents = JsonSerializer.Serialize(region, OPTIONS);

        File.WriteAllText(temp, contents);
        File.Replace(temp, target, null);
    }

    internal void DeleteRegion(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        //_logger.LogInformation("Deleting Region: {Id}", region.Id);

        var path = Path.Join(ContentPath, "Regions");

        File.Delete(Path.Join(path, $"{region.Id}.json"));
    }

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        //_logger.LogInformation("Register {Type}: {Id}", entity.GetType().Name, entity.Id);

        if (_entities[entity.GetType()].TryAdd(entity.Id, entity) == true)
        {
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

        //_logger.LogInformation("Unregister {Type}: {Id}", entity.GetType().Name, entity.Id);

        if (_entities[entity.GetType()].TryRemove(entity.Id, out _) == true)
        {
            _updateTimers[(uint)entity.Id.GetHashCode() % _updateTimers.Length].Unregister(entity);
        }
    }
}
