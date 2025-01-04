using System.Text.Json;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Object = Genesis.Core.Entities.Object;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private static readonly JsonSerializerOptions OPTIONS = new() { WriteIndented = true };

    private readonly string _contentPath;

    private readonly Dictionary<Type, Dictionary<Guid, Entity>> _entities = new()
    {
        { typeof(Object), [] }, { typeof(Player), [] }, { typeof(Region), [] },
    };

    private readonly ILogger<Manager> _logger;

    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[100];

    public Manager(ILogger<Manager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contentPath = configuration["Genesis:ContentPath"] ?? throw new Exception("ContentPath not defined");
    }

    /// <summary>
    /// Returns array of all registered entities of type T matching provided predicate.
    /// </summary>
    public T[] Find<T>(Func<T, bool> predicate) where T : Entity
    {
        return [.. _entities[typeof(T)].Values.Cast<T>().Where(predicate)];
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

        var path = Path.Join(_contentPath, "Players");

        var contents = JsonSerializer.Serialize(player, OPTIONS);

        File.WriteAllText(Path.Join(path, $"{player.Id}.json"), contents);
    }

    /// <summary>
    /// Save provided Region to file system.
    /// </summary>
    public void Save(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        var path = Path.Join(_contentPath, "Regions");

        var contents = JsonSerializer.Serialize(region, OPTIONS);

        File.WriteAllText(Path.Join(path, $"{region.Id}.json"), contents);
    }

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Register {type}: {id}", entity.GetType().Name, entity.Id);

        if (_entities[entity.GetType()].TryAdd(entity.Id, entity) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Register(entity);
        }
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(x => _updateTimers[x] = new(engine));

        var root = Path.Join(_contentPath, "Regions");

        foreach (string path in Directory.GetFiles(root, "*.json"))
        {
            JsonSerializer.Deserialize<Region>(File.ReadAllText(path))?.Load(engine);
        }

        _logger.LogInformation("Loaded {count} entities", _entities.Sum(list => list.Value.Count));

        _logger.LogInformation("Content Manager Started");
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(x => _updateTimers[x].Dispose());

        Find<Region>(x => true).ForEach(x => x.Unload(engine));

        _logger.LogInformation("Content Manager Stopped");
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Unregister {type}: {id}", entity.GetType().Name, entity.Id);

        if (_entities[entity.GetType()].Remove(entity.Id) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Unregister(entity);
        }
    }
}
