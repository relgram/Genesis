using System.Collections.Concurrent;

namespace Genesis.Core.Content;

internal sealed class UpdateTimer : IDisposable
{
    private readonly GameEngine _engine;
    private readonly ConcurrentDictionary<Guid, Entity> _entities = [];
    private readonly object _executeLock = new();
    private readonly Timer _timer;

    public UpdateTimer(GameEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _timer = new Timer(Elapsed, null, Random.Shared.Next(0, 100), 1_000);
    }

    private void Elapsed(object? sender)
    {
        lock (_executeLock)
        {
            foreach (var entity in _entities.Values)
            {
                _engine.Runtime.DoProcedure(_engine, "Update", "DoUpdate", entity);
            }
        }
    }

    public void Dispose() => _timer.Dispose();

    public void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.EntityId, entity) == false)
        {
            throw new ArgumentException($"Failed to register entity: {entity.EntityId}");
        }
    }

    public void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.EntityId, out var _) == false)
        {
            throw new ArgumentException($"Failed to unregister entity: {entity.EntityId}");
        }
    }
}
