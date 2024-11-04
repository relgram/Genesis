using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Item))]
public sealed class Item : Entity
{
    public Item(string name) : base(name)
    {

    }

    [NotMapped]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public Item[] Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Items.Find(x => IsMatch(x.Name, keyword), ref index) is Item item) return item;

        return base.FindMember(keyword, ref index);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        var effects = engine.Content.Query<Effect>(x => x.ParentId == EntityId);
        Parallel.ForEach(effects, effect => effect.Load(engine, this));

        var items = engine.Content.Query<Item>(x => x.ParentId == EntityId);
        Parallel.ForEach(items, item => item.Load(engine, this));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryAdd(effect.EntityId, effect) is true)
            {
                effect.Parent?.Unregister(effect);
                effect.Parent = this;
                return;
            }
        }

        if (entity is Item item)
        {
            if (_entities.TryAdd(item.EntityId, item) is true)
            {
                item.Parent?.Unregister(item);
                item.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryRemove(effect.EntityId, out var _) is true)
            {
                effect.Parent = null;
                return;
            }
        }

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
