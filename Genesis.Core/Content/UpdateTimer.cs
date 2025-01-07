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
        _timer = new Timer(Elapsed, null, Random.Shared.Next(0, 100), 1_000);
    }

    public void Dispose() => _timer.Dispose();

    public void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new Exception($"Failed to register [{entity.GetType().Name}]: {entity.Id}");
        }
    }

    public void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id, out _) == false)
        {
            throw new Exception($"Failed to unregister [{entity.GetType().Name}]: {entity.Id}");
        }
    }

    private void Elapsed(object? sender)
    {
        lock (_executeLock)
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Parent is null)
                {
                    _driver.Runtime.DoProcedure("Update", "OnUpdate", _driver, entity);
                }
                else
                {
                    _driver.Runtime.DoProcedure("Update", "OnUpdate", _driver, entity, entity.Parent);
                }
            }
        }
    }
}
