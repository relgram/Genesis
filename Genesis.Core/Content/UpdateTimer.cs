using System.Collections.Concurrent;

namespace Genesis.Core.Content;

internal sealed class UpdateTimer : IDisposable
{
    private readonly Driver _driver;
    private readonly ConcurrentDictionary<Guid, Entity> _entities = [];
    private readonly Lock _executeLock = new();
    private readonly Timer _timer;

    public UpdateTimer(Driver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _timer = new Timer(Elapsed, null, Random.Shared.Next(0, 1_000), 1_000);
    }

    public void Dispose()
    {
        lock (_executeLock)
        {
            _timer.Dispose();
        }
    }

    public void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException($"Failed to register [{entity.GetType().Name}]: {entity.Id}");
        }
    }

    public void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id, out _) == false)
        {
            throw new ArgumentException($"Failed to unregister [{entity.GetType().Name}]: {entity.Id}");
        }
    }

    private void Elapsed(object? sender)
    {
        lock (_executeLock)
        {
            foreach (var entity in _entities.Values)
            {
                _driver.Runtime.DoProcedure(entity, "Update", $"DoUpdate{entity.GetType().Name}");
            }
        }
    }
}
