using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Portal : Entity
{
    [JsonConstructor]
    public Portal(string name) : base(name)
    {
    }

    public ICollection<Effect> Effects
    {
        get => [.. Entities.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    public void Register(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Unregister(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }

    protected override void LoadMembers(Engine engine)
    {
        Effects.ForEach(x => x.Load(engine));
    }

    protected override void UnloadMembers(Engine engine)
    {
        Effects.ForEach(x => x.Unload(engine));
    }
}
