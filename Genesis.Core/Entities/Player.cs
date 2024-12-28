using System.Linq.Expressions;
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
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    public ICollection<Object> Objects
    {
        get => [.. _entities.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Objects.Find(x => IsMatch(x.Name, keyword), ref index) is Object @object) return @object;

        return base.FindMember(keyword, ref index);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        Effects.ForEach(effect => effect.Load(engine));
        Objects.ForEach(@object => @object.Load(engine));
    }

    protected override void UnloadMembers(GameEngine engine)
    {
        Effects.ForEach(effect => effect.Unload(engine));
        Objects.ForEach(@object => @object.Unload(engine));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryAdd(effect.EntityId, effect) == true)
            {
                effect.Parent?.Unregister(effect);
                effect.Parent = this;
                return;
            }
        }

        if (entity is Object @object)
        {
            if (_entities.TryAdd(@object.EntityId, @object) == true)
            {
                @object.Parent?.Unregister(@object);
                @object.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public void Save(GameEngine engine) => engine.Content.Save(this);

    public static Player[] Search(GameEngine engine, Expression<Func<Player, bool>> predicate) => engine.Content.Search(predicate);

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Effect effect)
        {
            if (_entities.TryRemove(effect.EntityId) == true)
            {
                effect.Parent = null;
                return;
            }
        }

        if (entity is Object @object)
        {
            if (_entities.TryRemove(@object.EntityId) == true)
            {
                @object.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
