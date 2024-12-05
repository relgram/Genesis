using System.Text.Json.Serialization;
using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Room : Entity
{
    public Room(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Item> Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init => value.ForEach(Register);
    }

    [JsonInclude]
    public ICollection<Player> Players
    {
        get => [.. _entities.Values.OfType<Player>()];
        internal init => value.ForEach(Register);
    }

    [Member]
    public ICollection<Portal> Portals
    {
        get => [.. _entities.Values.OfType<Portal>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Portals.Where(x => x.IsEnabled).Find(x => IsMatch(x.Name, keyword), ref index) is Portal portal) return portal;

        if (Players.Where(x => x.IsEnabled).Find(x => IsMatch(x.Name, keyword), ref index) is Player player) return player;

        if (Items.Where(x => x.IsEnabled).Find(x => IsMatch(x.Name, keyword), ref index) is Item item) return item;

        return base.FindMember(keyword, ref index);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Actor actor)
        {
            if (_entities.TryAdd(actor.EntityId, actor) == true)
            {
                actor.Parent?.Unregister(actor);
                actor.Parent = this;
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

        if (entity is Player player)
        {
            if (_entities.TryAdd(player.EntityId, player) == true)
            {
                player.Parent?.Unregister(player);
                player.Parent = this;
                return;
            }
        }

        if (entity is Portal portal)
        {
            if (_entities.TryAdd(portal.EntityId, portal) == true)
            {
                portal.Parent?.Unregister(portal);
                portal.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Actor actor)
        {
            if (_entities.TryRemove(actor.EntityId, out var _) == true)
            {
                actor.Parent = null;
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

        if (entity is Player player)
        {
            if (_entities.TryRemove(player.EntityId, out var _) == true)
            {
                player.Parent = null;
                return;
            }
        }

        if (entity is Portal portal)
        {
            if (_entities.TryRemove(portal.EntityId, out var _) == true)
            {
                portal.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
