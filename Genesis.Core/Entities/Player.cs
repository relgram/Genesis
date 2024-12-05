using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Player : Entity
{
    public Player(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
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

    public void Disconnect(GameEngine engine, string message = "Disconnected")
    {
        Client?.Disconnect(engine, message);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryAdd(effect.EntityId, effect) == true)
            {
                effect.Parent?.Unregister(effect);
                effect.Parent = this;
                return;
            }
        }

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

    public void SendBytes(byte[] bytes)
    {
        Client?.SendBytes(bytes);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryRemove(effect.EntityId, out var _) == true)
            {
                effect.Parent = null;
                return;
            }
        }

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
