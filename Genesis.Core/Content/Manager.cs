using System.Collections.Concurrent;
using System.Text.Json;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

    private readonly string _contentPath;
    private readonly Dictionary<Type, ConcurrentDictionary<Guid, Entity>> _entities = [];
    private readonly ILogger<Manager> _logger;
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[1000];

    public Manager(ILogger<Manager> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contentPath = configuration["Genesis:ContentPath"] ?? throw new Exception("ContentPath not defined");

        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(x => x.IsAbstract is false))
        {
            if (type.IsAssignableTo(typeof(Entity)) is true)
            {
                _entities[type] = [];
            }
        }
    }

    /// <summary>
    /// Disable updates on registered entity
    /// </summary>
    internal void Disable(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(nameof(entity));

        _logger.LogInformation("Disable [{type}]: {entityId}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].ContainsKey(entity.EntityId) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Unregister(entity);
        }
        else
        {
            throw new InvalidOperationException($"Failed to disable [{entity.GetType().Name}]: {entity.EntityId}");
        }
    }

    /// <summary>
    /// Enables updates on registered entity
    /// </summary>
    internal void Enable(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(nameof(entity));

        _logger.LogInformation("Enable [{type}]: {entityId}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].ContainsKey(entity.EntityId) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Register(entity);
        }
        else
        {
            throw new InvalidOperationException($"Failed to enable [{entity.GetType().Name}]: {entity.EntityId}");
        }
    }

    /// <summary>
    /// Registers entity with manager
    /// </summary>
    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities[entity.GetType()].TryAdd(entity.EntityId, entity) == false)
        {
            throw new InvalidOperationException($"Failed to register [{entity.GetType().Name}]: {entity.EntityId}");
        }
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        Enumerable.Range(0, _updateTimers.Length).ForEach(x => _updateTimers[x] = new(engine));

        if (!File.Exists(_contentPath))
        {
            File.WriteAllText(_contentPath, "[]");
        }

        using var stream = File.OpenRead(_contentPath);
        JsonSerializer.Deserialize<Zone[]>(stream)?.ForEach(x => x.Load(engine));
        _entities[typeof(Zone)].Values.Cast<Zone>().ForEach(zone => zone.Enable(engine));
        _logger.LogInformation("Loaded {count} entities", _entities.Sum(list => list.Value.Count));
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        _updateTimers.ForEach(x => x.Dispose());
        Find<Zone>(x => true).ForEach(x => x.Disable(engine));
        using var stream = File.Open(_contentPath, FileMode.Create);
        JsonSerializer.Serialize(stream, Find<Zone>(x => true), JSON_OPTIONS);
    }

    /// <summary>
    /// Unregisters entity with manager and removes from assigned update timer
    /// </summary>
    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities[entity.GetType()].TryRemove(entity.EntityId) == false)
        {
            throw new InvalidOperationException($"Failed to unregister [{entity.GetType().Name}]: {entity.EntityId}");
        }
    }

    /// <summary>
    /// Returns array of all registered entities of type T matching provided predicate
    /// </summary>
    public T[] Find<T>(Func<T, bool> predicate) where T : Entity
    {
        return [.. _entities[typeof(T)].Values.Cast<T>().Where(predicate)];
    }

    /// <summary>
    /// Returns registered entity of type T with specified entityId
    /// </summary>
    public T? Get<T>(Guid entityId) where T : Entity
    {
        return _entities[typeof(T)].GetValueOrDefault(entityId) as T;
    }
}
