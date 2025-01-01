using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Region))]
public sealed class Region : Entity
{
    private readonly ConcurrentDictionary<Guid, Entity> _entities = [];
    private readonly ConcurrentDictionary<Guid, Player> _players = [];

    public Region(string name) : base(name)
    {
    }

    internal override ICollection<Entity> Entities
    {
        get => [.. _entities.Values.OfType<Entity>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Mobile> Mobiles
    {
        get => [.. _entities.Values.OfType<Mobile>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _entities.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Player> Players
    {
        get => [.. _players.Values.OfType<Player>()];
    }

    protected override void LoadMembers(GameEngine engine)
    {
        Objects.ForEach(entity => entity.Load(engine));
    }

    protected override void UnloadMembers(GameEngine engine)
    {
        Objects.ForEach(entity => entity.Unload(engine));
    }

    private void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Entity Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Objects.Find(x => IsMatch(x.Name, keyword), ref index) is Object @object) return @object;

        if (Mobiles.Find(x => IsMatch(x.Name, keyword), ref index) is Mobile mobile) return mobile;

        if (Players.Find(x => IsMatch(x.Name, keyword), ref index) is Player player) return player;

        return null;
    }

    public void Register(Mobile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Mobile Already Registered");
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

    public void Register(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_players.TryAdd(entity.Id, entity) == true)
        {
            entity.Parent?.Unregister(entity);
            entity.Parent = this;
            return;
        }
    }

    public static Region[] Seek(GameEngine engine, Expression<Func<Region, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return engine.Content.Seek(predicate);
    }

    public void Unregister(Mobile entity)
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

    public void Unregister(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_players.TryRemove(entity.Id) == true)
        {
            entity.Parent = null;
            return;
        }
    }
}
