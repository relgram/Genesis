using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Zone))]
public sealed class Zone : Entity
{
    public Zone(string name) : base(name)
    {
    }

    [NotMapped]
    public ICollection<Area> Areas
    {
        get => [.. _entities.Values.OfType<Area>()];
        init => value.ForEach(Register);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        var areas = engine.Content.Query<Area>(x => x.ParentId == EntityId);
        Parallel.ForEach(areas, area => area.Load(engine, this));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Area area)
        {
            if (_entities.TryAdd(area.EntityId, area) is true)
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
            if (_entities.TryRemove(area.EntityId, out var _) is true)
            {
                area.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
