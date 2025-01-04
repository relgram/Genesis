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

    public void Register(Player entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public override void Unload(GameEngine engine)
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

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Objects.Find(x => IsMatch(x.Name, keyword), ref index) is Object @object)
        {
            return @object;
        }

        if (Players.Find(x => IsMatch(x.Name, keyword), ref index) is Player player)
        {
            return player;
        }

        return null;
    }

    protected override void LoadMembers(GameEngine engine)
    {
        Effects.ForEach(x => x.Load(engine));
        Mobiles.ForEach(x => x.Load(engine));
        Objects.ForEach(x => x.Load(engine));
    }

    protected override void UnloadMembers(GameEngine engine)
    {
        Effects.ForEach(x => x.Unload(engine));
        Mobiles.ForEach(x => x.Unload(engine));
        Objects.ForEach(x => x.Unload(engine));
    }
}
