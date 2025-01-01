using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

public sealed class Mobile : Entity
{
    [JsonConstructor]
    public Mobile(string name) : base(name)
    {
    }

    [NotMapped]
    public ICollection<Effect> Effects
    {
        get => [.. _entities.Values.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. _entities.Values.OfType<Object>()];
        init => value.ForEach(Register);
    }

    public void Register(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == true)
        {
            entity.Parent?.Unregister(entity);
            entity.Parent = this;
            return;
        }
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryAdd(entity.Id, entity) == true)
        {
            entity.Parent?.Unregister(entity);
            entity.Parent = this;
            return;
        }
    }

    public void Unregister(Effect entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id) == false)
        {
            throw new ArgumentException("Effect Not Registered");
        }

        entity.Parent = null;

        return;
    }

    public void Unregister(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.TryRemove(entity.Id) == false)
        {
            throw new ArgumentException("Object Not Registered");
        }

        entity.Parent = null;

        return;
    }
}
