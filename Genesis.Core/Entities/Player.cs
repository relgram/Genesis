using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Player : Entity
{
    [JsonConstructor]
    public Player(string name) : base(name)
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

    public override void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.SavePlayer(this);

        base.Unload(engine);
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

    protected override void LoadMembers(GameEngine engine)
    {
        Effects.ForEach(x => x.Load(engine));
        Objects.ForEach(x => x.Load(engine));
    }

    protected override void UnloadMembers(GameEngine engine)
    {
        Effects.ForEach(x => x.Unload(engine));
        Objects.ForEach(x => x.Unload(engine));
    }
}
