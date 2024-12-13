using System.Text.Json.Serialization;
using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Mortal : Entity
{
    [JsonConstructor]
    public Mortal(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [Member]
    public ICollection<Widget> Widgets
    {
        get => [.. _entities.Values.OfType<Widget>()];
        init => value.ForEach(Register);
    }

    protected override Entity? FindMember(string keyword, ref int index)
    {
        static bool IsMatch(string name, string value) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith(value, true, null));

        if (Widgets.Find(x => IsMatch(x.Name, keyword), ref index) is Widget obj) return obj;

        return base.FindMember(keyword, ref index);
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

        if (entity is Widget widget)
        {
            if (_entities.TryAdd(widget.EntityId, widget) == true)
            {
                widget.Parent?.Unregister(widget);
                widget.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

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

        if (entity is Widget widget)
        {
            if (_entities.TryRemove(widget.EntityId) == true)
            {
                widget.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}