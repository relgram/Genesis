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
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[1000];

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

    /// <summary>
    /// Registers entity with manager and assigns to update timer
    /// </summary>
    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Register: [{type}] {entity}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].TryAdd(entity.EntityId, entity) is true)
        {
            var index = (uint)entity.GetHashCode() % _updateTimers.Length;

            _updateTimers[index].Register(entity);
        }
    }

    internal void Save(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(nameof(entity));

        _logger.LogInformation("Saving: [{type}] {entity}", entity.GetType().Name, entity.EntityId);

        using var context = _contextFactory.CreateDbContext();

        context.Upsert(entity).Run();
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        Enumerable.Range(0, _updateTimers.Length).ForEach(index => _updateTimers[index] = new(engine));

        Parallel.ForEach(Query<Zone>(x => true), zone => zone.Load(engine, null));
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();

        Enumerable.Range(0, _updateTimers.Length).ForEach(index => _updateTimers[index].Dispose());

        Parallel.ForEach(Find<Zone>(x => true), zone => zone.Save(engine, true));
    }

    /// <summary>
    /// Unregisters entity with manager and removes from assigned update timer
    /// </summary>
    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Unregister: [{type}] {entity}", entity.GetType().Name, entity.EntityId);

        if (_entities[entity.GetType()].TryRemove(entity.EntityId, out var _) is true)
        {
            var index = (uint)entity.GetHashCode() % _updateTimers.Length;

            _updateTimers[index].Unregister(entity);
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
    /// Returns array of all database entities of type T matching provided predicate
    /// </summary>
    public T[] Query<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        using var context = _contextFactory.CreateDbContext();
        return [.. context.Set<T>().AsNoTracking().Where(predicate)];
    }
}
