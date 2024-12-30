using System.Collections.Concurrent;
using System.Linq.Expressions;
using Genesis.Core.Content.Database;
using Genesis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private const string CONNECTION_STRING = @"Server=(local);Database=Shadowlance;TrustServerCertificate=true;Trusted_Connection=True;";

    private readonly PooledDbContextFactory<EntityContext> _contextFactory;
    private readonly Dictionary<Type, ConcurrentDictionary<Guid, Entity>> _entities = [];
    private readonly ILogger<Manager> _logger;
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[100];

    public Manager(ILogger<Manager> logger)
    {
        var builder = new DbContextOptionsBuilder<EntityContext>().UseSqlServer(CONNECTION_STRING);
        _contextFactory = new PooledDbContextFactory<EntityContext>(builder.Options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(x => x.IsAbstract is false))
        {
            if (type.IsAssignableTo(typeof(Entity)) is true)
            {
                _entities[type] = [];
            }
        }
    }

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        //_logger.LogInformation("Register {type}: {entityId}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].TryAdd(entity.EntityId, entity) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Register(entity);
        }
    }

    internal void Save(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        //_logger.LogInformation("Saving: [{type}] {entity}", entity.GetType().Name, entity.EntityId);

        using var context = _contextFactory.CreateDbContext();

        context.Upsert(entity).Run();
    }

    internal T[] Seek<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        using var context = _contextFactory.CreateDbContext();
        return [.. context.Set<T>().AsNoTracking().Where(predicate)];
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        Enumerable.Range(0, _updateTimers.Length).ForEach(i => _updateTimers[i] = new(engine));

        Parallel.ForEach(Seek<Region>(x => true), x => x.Load(engine));
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        Enumerable.Range(0, _updateTimers.Length).ForEach(i => _updateTimers[i].Dispose());

        Parallel.ForEach(Find<Region>(x => true), x => x.Save(engine));
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Unregister {type}: {entityId}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].TryRemove(entity.EntityId) == true)
        {
            _updateTimers[(uint)entity.GetHashCode() % _updateTimers.Length].Unregister(entity);
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
