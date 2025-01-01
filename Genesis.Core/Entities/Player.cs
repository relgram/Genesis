using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Player))]
public sealed class Player : Entity
{
    public Player(string name) : base(name)
    {
    }

    [NotMapped]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _entities.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Objects.Find(x => IsMatch(x.Name, keyword), ref index) is Object @object) return @object;

        return null;
    }

    public void Register(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Effect Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Object Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    public static Player[] Seek(GameEngine engine, Expression<Func<Player, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return engine.Content.Seek(predicate);
    }

    public override void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Save(this);

        base.Unload(engine);
    }

    public void Unregister(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id) == true)
        {
            entity.Parent = null;
            return;
        }
    }

    public void Unregister(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id) == true)
        {
            entity.Parent = null;
            return;
        }
    }
}
