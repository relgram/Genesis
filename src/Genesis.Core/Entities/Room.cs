using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Room))]
public sealed class Room : Entity
{
    public Room(string name) : base(name)
    {
    }

    [NotMapped]
    public Item[] Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init => value.ForEach(Register);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        var items = engine.Content.Query<Item>(x => x.ParentId == EntityId);
        Parallel.ForEach(items, item => item.Load(engine, this));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Item item)
        {
            if (_entities.TryAdd(item.EntityId, item) is true)
            {
                item.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Item item)
        {
            if (_entities.TryRemove(item.EntityId, out var _) is true)
            {
                item.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
