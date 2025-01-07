using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Region : Entity
{
    [JsonConstructor]
    public Region(string name) : base(name)
    {
    }

    public ICollection<Effect> Effects
    {
        get => [.. Entities.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    public ICollection<Mobile> Mobiles
    {
        get => [.. Entities.OfType<Mobile>()];
        init => value.ForEach(Register);
    }

    public ICollection<Object> Objects
    {
        get => [.. Entities.OfType<Object>()];
        init => value.ForEach(Register);
    }

    [JsonIgnore]
    public ICollection<Player> Players
    {
        get => [.. Entities.OfType<Player>()];
    }

    public ICollection<Portal> Portals
    {
        get => [.. Entities.OfType<Portal>()];
        init => value.ForEach(Register);
    }

    public void Register(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Register(Mobile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Register(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Register(Portal entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public override void Unload(Engine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.SaveRegion(this);

        base.Unload(engine);
    }

    public void Unregister(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    public void Unregister(Mobile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    public void Unregister(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    public void Unregister(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    public void Unregister(Portal entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    protected override void LoadMembers(Engine engine)
    {
        Effects.ForEach(x => x.Load(engine));
        Mobiles.ForEach(x => x.Load(engine));
        Objects.ForEach(x => x.Load(engine));
        Portals.ForEach(x => x.Load(engine));
    }

    protected override void UnloadMembers(Engine engine)
    {
        Effects.ForEach(x => x.Unload(engine));
        Mobiles.ForEach(x => x.Unload(engine));
        Objects.ForEach(x => x.Unload(engine));
        Portals.ForEach(x => x.Unload(engine));
    }
}
