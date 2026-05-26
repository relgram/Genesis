using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Player : Entity
{
    [JsonConstructor]
    public Player(string name) : base(name)
    {
    }

    public IReadOnlyCollection<Effect> Effects
    {
        get
        {
            var list = new List<Effect>();
            foreach (var entity in Entities)
            {
                if (entity is Effect effect) list.Add(effect);
            }
            return new ReadOnlyCollection<Effect>(list);
        }
        init => value.ForEach(Register);
    }

    public IReadOnlyCollection<Object> Objects
    {
        get
        {
            var list = new List<Object>();
            foreach (var entity in Entities)
            {
                if (entity is Object obj) list.Add(obj);
            }
            return new ReadOnlyCollection<Object>(list);
        }
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

    public override void Unload(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        driver.Content.Save(this);

        base.Unload(driver);
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
}
