using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Locker : Entity
{
    public Locker(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Item> Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Items.Where(x => x.IsEnabled).Find(x => IsMatch(x.Name, keyword), ref index) is Item item) return item;

        return base.FindMember(keyword, ref index);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Item item)
        {
            if (_entities.TryAdd(item.EntityId, item) == true)
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

        if (entity is Item item)
        {
            if (_entities.TryRemove(item.EntityId, out var _) == true)
            {
                item.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
