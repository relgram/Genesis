using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Object : Entity
{
    [JsonConstructor]
    public Object(string name) : base(name)
    {
    }

    public ICollection<Effect> Effects
    {
        get => [.. Entities.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    public ICollection<Object> Objects
    {
        get => [.. Entities.OfType<Object>()];
        init => value.ForEach(Register);
    }

    public void Register(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Unregister(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    public void Unregister(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    protected override void LoadMembers(Driver driver)
    {
        Effects.ForEach(x => x.Load(driver));
        Objects.ForEach(x => x.Load(driver));
    }

    protected override void UnloadMembers(Driver driver)
    {
        Effects.ForEach(x => x.Unload(driver));
        Objects.ForEach(x => x.Unload(driver));
    }
}
