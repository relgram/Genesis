using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Region))]
public sealed class Region : Entity
{
    public Region(string name) : base(name)
    {
    }

    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    public ICollection<Mobile> Mobiles
    {
        get => [.. _entities.Values.OfType<Mobile>()];
        init => value.ForEach(Register);
    }

    public ICollection<Object> Objects
    {
        get => [.. _entities.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Player> Players
    {
        get => [.. _entities.Values.OfType<Player>()];
    }

    public ICollection<Portal> Portals
    {
        get => [.. _entities.Values.OfType<Portal>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Portals.Find(x => IsMatch(x.Name, keyword), ref index) is Portal portal) return portal;

        if (Objects.Find(x => IsMatch(x.Name, keyword), ref index) is Object @object) return @object;

        if (Players.Find(x => IsMatch(x.Name, keyword), ref index) is Player player) return player;

        return base.FindMember(keyword, ref index);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        Effects.ForEach(effect => effect.Load(engine));
        Mobiles.ForEach(mobile => mobile.Load(engine));
        Objects.ForEach(@object => @object.Load(engine));
        Portals.ForEach(portal => portal.Load(engine));
    }

    protected override void UnloadMembers(GameEngine engine)
    {
        Effects.ForEach(effect => effect.Unload(engine));
        Mobiles.ForEach(mobile => mobile.Unload(engine));
        Objects.ForEach(@object => @object.Unload(engine));
        Portals.ForEach(portal => portal.Unload(engine));
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

        if (entity is Mobile mobile)
        {
            if (_entities.TryAdd(mobile.EntityId, mobile) == true)
            {
                mobile.Parent?.Unregister(mobile);
                mobile.Parent = this;
                return;
            }
        }

        if (entity is Object @object)
        {
            if (_entities.TryAdd(@object.EntityId, @object) == true)
            {
                @object.Parent?.Unregister(@object);
                @object.Parent = this;
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

    public void Save(GameEngine engine) => engine.Content.Save(this);

    public static Region[] Search(GameEngine engine, Expression<Func<Region, bool>> predicate) => engine.Content.Search(predicate);

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryRemove(effect.EntityId) == true)
            {
                effect.Parent = null;
                return;
            }
        }

        if (entity is Mobile mobile)
        {
            if (_entities.TryRemove(mobile.EntityId) == true)
            {
                mobile.Parent = null;
                return;
            }
        }

        if (entity is Object @object)
        {
            if (_entities.TryRemove(@object.EntityId) == true)
            {
                @object.Parent = null;
                return;
            }
        }

        if (entity is Player player)
        {
            if (_entities.TryRemove(player.EntityId) == true)
            {
                player.Parent = null;
                return;
            }
        }

        if (entity is Portal portal)
        {
            if (_entities.TryRemove(portal.EntityId) == true)
            {
                portal.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
