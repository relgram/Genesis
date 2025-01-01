using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics;
using Genesis.Core.Content.Database;
using Genesis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Object = Genesis.Core.Entities.Object;

namespace Genesis.Core.Content;

public sealed class Manager
{
    private const string CONNECTION_STRING = @"Server=(local);Database=Shadowlance;TrustServerCertificate=true;Trusted_Connection=True;";

    private readonly PooledDbContextFactory<DataContext> _contextFactory;
    private readonly Dictionary<Type, ConcurrentDictionary<Guid, Entity>> _entities = new()
    {
        { typeof(Object), [] }, { typeof(Player), [] }, { typeof(Region), [] }
    };
    private readonly ILogger<Manager> _logger;
    private readonly UpdateTimer[] _updateTimers = new UpdateTimer[100];

    public Manager(ILogger<Manager> logger)
    {
        var builder = new DbContextOptionsBuilder<DataContext>().UseSqlServer(CONNECTION_STRING);
        _contextFactory = new PooledDbContextFactory<DataContext>(builder.Options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        using var context = _contextFactory.CreateDbContext();
        _ = context.Regions.FirstOrDefault();
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

    internal T[] Seek<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        using var context = _contextFactory.CreateDbContext();
        return [.. context.Set<T>().AsNoTracking().Where(predicate)];
    }

    internal void Start(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(i => _updateTimers[i] = new(engine));

        Parallel.ForEach(Seek<Region>(x => true), x => x.Load(engine));

        _logger.LogInformation("Content Manager Started");
    }

    internal void Stop(GameEngine engine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(engine);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Stopping Content Manager...");

        Enumerable.Range(0, _updateTimers.Length).ForEach(i => _updateTimers[i].Dispose());

        Parallel.ForEach(Find<Region>(x => true), Save);

        Parallel.ForEach(Find<Region>(x => true), x => x.Unload(engine));

        _logger.LogInformation("Content Manager Stopped");
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogInformation("Unregister {type}: {id}", entity.GetType().Name, entity.Id);

        if (_entities[entity.GetType()].TryRemove(entity.Id, out _) == true)
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

    /// <summary>
    /// Save provided Player to database
    /// </summary>
    public void Save(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        _logger.LogInformation("Save {type}: {id}", player.GetType().Name, player.Id);

        using var context = _contextFactory.CreateDbContext();

        context.Players.Upsert(player).Run();
    }

    /// <summary>
    /// Save provided Region to database
    /// </summary>
    public void Save(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        _logger.LogInformation("Save {type}: {id}", region.GetType().Name, region.Id);

        using var context = _contextFactory.CreateDbContext();

        context.Regions.Upsert(region).Run();
    }
}
