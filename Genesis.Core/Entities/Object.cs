using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Object : Entity
{
    private readonly HashSet<Entity> _internal = [];

    [JsonConstructor]
    public Object(string name) : base(name)
    {
    }

    internal override HashSet<Entity> Entities
    {
        get => [.. _internal];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _internal.OfType<Object>()];
        init => value.ForEach(Register);
    }

    public void Load(GameEngine engine, Region region)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(region);

        engine.Content.Register(this);

        region.Register(this);
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

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.Add(entity) == true)
        {
            entity.Parent?.Unregister(entity);
            entity.Parent = this;
            return;
        }
    }
}
