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
        get => [.. Entities.OfType<Effect>()];
        init => value.ForEach(Register);
    }

    [NotMapped]
    public ICollection<Object> Objects
    {
        get => [.. Entities.OfType<Object>()];
        init => value.ForEach(Register);
    }

    public void Register(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Register(entity);
    }

    public void Unregister(Object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        base.Unregister(entity);
    }
}
