using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Region))]
public sealed class Region : Entity
{
    private readonly ConcurrentDictionary<Guid, Entity> _internal = [];
    private readonly ConcurrentDictionary<Guid, Player> _players = [];

    public Region(string name) : base(name)
    {
    }

    internal override ICollection<Entity> Entities
    {
        get => _internal.Values;
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Mobile> Mobiles
    {
        get => [.. _internal.Values.OfType<Mobile>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _internal.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Player> Players
    {
        get => [.. _players.Values.OfType<Player>()];
    }

    private void LoadMembers(GameEngine engine)
    {
        Parallel.ForEach(Objects, x => x.Load(engine, this));
    }

    public void Load(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Register(this);

        LoadMembers(engine);
    }

    internal override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Mobile mobile)
        {
            Register(mobile);
            return;
        }

        if (entity is Object @object)
        {
            Register(@object);
            return;
        }

        if (entity is Player player)
        {
            Register(player);
            return;
        }
    }

    public void Register(Mobile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Mobile Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Object Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    public void Register(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_players.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Player Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }

    public void Save(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Save(this);
    }
}
