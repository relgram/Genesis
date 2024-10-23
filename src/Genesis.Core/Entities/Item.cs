using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Item))]
public sealed class Item : Entity
{
    [NotMapped]
    public Item[] Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init
        {
            foreach (var item in value)
            {
                if (_entities.TryAdd(item.EntityId, item) is false)
                {
                    throw new ArgumentException($"Failed to register item: {item.EntityId}");
                }
            }
        }
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
