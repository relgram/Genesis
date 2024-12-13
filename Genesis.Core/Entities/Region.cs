using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Genesis.Core.Content;
using Genesis.Core.Content.Database;
using Genesis.Core.Entities.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Genesis.Core.Entities;

[Table(nameof(Region))]
public sealed class Region : Entity
{
    public Region(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [Member]
    public ICollection<Mortal> Mortals
    {
        get => [.. _entities.Values.OfType<Mortal>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Player> Players
    {
        get => [.. _entities.Values.OfType<Player>()];
    }

    [Member]
    public ICollection<Portal> Portals
    {
        get => [.. _entities.Values.OfType<Portal>()];
        init => value.ForEach(Register);
    }

    [Member]
    public ICollection<Widget> Widgets
    {
        get => [.. _entities.Values.OfType<Widget>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Portals.Find(x => IsMatch(x.Name, keyword), ref index) is Portal portal) return portal;

        if (Players.Find(x => IsMatch(x.Name, keyword), ref index) is Player player) return player;

        if (Widgets.Find(x => IsMatch(x.Name, keyword), ref index) is Widget widget) return widget;

        return base.FindMember(keyword, ref index);
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

        if (entity is Mortal mortal)
        {
            if (_entities.TryAdd(mortal.EntityId, mortal) == true)
            {
                mortal.Parent?.Unregister(mortal);
                mortal.Parent = this;
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

        if (entity is Widget widget)
        {
            if (_entities.TryAdd(widget.EntityId, widget) == true)
            {
                widget.Parent?.Unregister(widget);
                widget.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public void Save(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        engine.Content.Save(this);
    }

    public static Region[] Search(GameEngine engine, Expression<Func<Region, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return engine.Content.Search(predicate);
    }

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

        if (entity is Mortal mortal)
        {
            if (_entities.TryRemove(mortal.EntityId) == true)
            {
                mortal.Parent = null;
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

        if (entity is Widget widget)
        {
            if (_entities.TryRemove(widget.EntityId) == true)
            {
                widget.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
