using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Mobile : Entity
{
    private readonly ConcurrentDictionary<Guid, Entity> _internal = [];

    [JsonConstructor]
    public Mobile(string name) : base(name)
    {
    }

    internal override ICollection<Entity> Entities
    {
        get => _internal.Values;
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _internal.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    internal override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Object @object)
        {
            Register(@object);
            return;
        }
    }

    public void Load(GameEngine engine, Region parent)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(parent);

        engine.Content.Register(this);

        parent.Register(this);
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.TryAdd(entity.Id, entity) == false)
        {
            throw new ArgumentException("Object Already Registered");
        }

        entity.Parent?.Unregister(entity);

        entity.Parent = this;
    }
}
