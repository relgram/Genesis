using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Genesis.Core.Content.Serialization;
using Genesis.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private readonly string _contentPath;
    private readonly Dictionary<Type, ConcurrentDictionary<Guid, Entity>> _entities = [];
    private readonly ILogger<Manager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Timer _saveTimer;
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[1000];

    public Manager(ILogger<Manager> logger, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _contentPath = configuration["Genesis:ContentPath"] ?? throw new Exception("ContentPath not defined");
        _saveTimer = new(SaveCallback, null, Timeout.Infinite, Timeout.Infinite);

        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(x => x.IsAbstract is false))
        {
            if (type.IsAssignableTo(typeof(Entity)) is true)
            {
                _entities[type] = [];
            }
        }
    }

    private void SaveCallback(object? state)
    {
        var timer = Stopwatch.StartNew();
        var zones = Find<Zone>(x => x is not null);
        using var stream = File.Open(_contentPath, FileMode.Create);
        JsonSerializer.Serialize(stream, zones, EntityContext.Default.ZoneArray);
        _logger.LogInformation("Saved entities in {duration}ms", timer.ElapsedMilliseconds);
        Process.Start(new ProcessStartInfo("git.exe", "commit -a -m \"Update Content.json\"")
        {
            WorkingDirectory = Path.GetDirectoryName(_contentPath)
        });
    }

    /// <summary>
    /// Disable updates on registered entity
    /// </summary>
    internal void Disable(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities[entity.GetType()].ContainsKey(entity.EntityId) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Unregister(entity);
        }
    }

    /// <summary>
    /// Enables updates on registered entity
    /// </summary>
    internal void Enable(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities[entity.GetType()].ContainsKey(entity.EntityId) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Register(entity);
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
            _logger.LogWarning("Failed to register [{type}]: {entityId}", entity.GetType().Name, entity.EntityId);
        }
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        for (int i = 0; i < _updateTimers.Length; ++i)
        {
            var logger = _loggerFactory.CreateLogger<UpdateTimer>();
            _updateTimers[i] = new(logger, engine);
        }

        var timer = Stopwatch.StartNew();
        var context = EntityContext.Default.ZoneArray;
        using var stream = File.Open(_contentPath, FileMode.Open);
        JsonSerializer.Deserialize(stream, context)?.ForEach(x => x.Load(engine));
        _entities[typeof(Zone)].Values.Cast<Zone>().ForEach(zone => zone.Enable(engine));
        _logger.LogInformation("Loaded {count} entities", _entities.Sum(list => list.Value.Count));
        _saveTimer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        _updateTimers.ForEach(x => x.Dispose());
        _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Find<Zone>(zone => zone.IsEnabled).ForEach(zone => zone.Disable(engine));

        var timer = Stopwatch.StartNew();
        var zones = Find<Zone>(x => x is not null);
        using var stream = File.Open(_contentPath, FileMode.Create);
        JsonSerializer.Serialize(stream, zones, EntityContext.Default.ZoneArray);
        _logger.LogInformation("Saved entities in {duration}ms", timer.ElapsedMilliseconds);
        Process.Start(new ProcessStartInfo("git.exe", "commit -a -m \"Update Content.json\"")
        {
            WorkingDirectory = Path.GetDirectoryName(_contentPath)
        });
    }

    /// <summary>
    /// Unregisters entity with manager and removes from assigned update timer
    /// </summary>
    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities[entity.GetType()].TryRemove(entity.EntityId) == false)
        {
            _logger.LogWarning("Failed to unregister [{type}]: {entityId}", entity.GetType().Name, entity.EntityId);
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
