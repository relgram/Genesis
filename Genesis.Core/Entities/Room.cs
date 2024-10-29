using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Room))]
public sealed class Room : Entity
{
    public Room(string name) : base(name)
    {
    }

    [NotMapped]
    public Actor[] Actors
    {
        get => [.. _entities.Values.OfType<Actor>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public Item[] Items
    {
        get => [.. _entities.Values.OfType<Item>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public Player[] Players
    {
        get => [.. _entities.Values.OfType<Player>()];
    }

    [NotMapped]
    public Portal[] Portals
    {
        get => [.. _entities.Values.OfType<Portal>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Portals.Find(x => IsMatch(x.Name, keyword), ref index) is Portal portal) return portal;

        if (Players.Find(x => IsMatch(x.Name, keyword), ref index) is Player player) return player;

        if (Items.Find(x => IsMatch(x.Name, keyword), ref index) is Item item) return item;

        return base.FindMember(keyword, ref index);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        var actors = engine.Content.Query<Actor>(x => x.ParentId == EntityId);
        Parallel.ForEach(actors, actor => actor.Load(engine, this));

        var items = engine.Content.Query<Item>(x => x.ParentId == EntityId);
        Parallel.ForEach(items, item => item.Load(engine, this));

        var portals = engine.Content.Query<Portal>(x => x.ParentId == EntityId);
        Parallel.ForEach(portals, portal => portal.Load(engine, this));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Actor actor)
        {
            if (_entities.TryAdd(actor.EntityId, actor) is true)
            {
                actor.Parent?.Unregister(actor);
                actor.Parent = this;
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

        if (entity is Player player)
        {
            if (_entities.TryAdd(player.EntityId, player) is true)
            {
                player.Parent?.Unregister(player);
                player.Parent = this;
                return;
            }
        }

        if (entity is Portal portal)
        {
            if (_entities.TryAdd(portal.EntityId, portal) is true)
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
            if (_entities.TryRemove(actor.EntityId, out var _) is true)
            {
                actor.Parent = null;
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

        if (entity is Player player)
        {
            if (_entities.TryRemove(player.EntityId, out var _) is true)
            {
                player.Parent = null;
                return;
            }
        }

        if (entity is Portal portal)
        {
            if (_entities.TryRemove(portal.EntityId, out var _) is true)
            {
                portal.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
