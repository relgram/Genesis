using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Zone : Entity
{
    public Zone(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Area> Areas
    {
        get => [.. _entities.Values.OfType<Area>()];
        init => value.ForEach(Register);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Area area)
        {
            if (_entities.TryAdd(area.EntityId, area) == true)
            {
                area.Parent?.Unregister(area);
                area.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Area area)
        {
            if (_entities.TryRemove(area.EntityId, out var _) == true)
            {
                area.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
