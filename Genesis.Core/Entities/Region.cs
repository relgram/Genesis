using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Region))]
public sealed class Region : Entity
{
    private readonly HashSet<Entity> _internal = [];

    public Region(string name) : base(name)
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

    private void LoadMembers(GameEngine engine)
    {
        Objects.ForEach(entity => entity.Load(engine, this));
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

    public void Save(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Save(this);
    }
}
