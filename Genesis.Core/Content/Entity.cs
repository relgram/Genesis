using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Genesis.Core.Entities;
using Object = Genesis.Core.Entities.Object;

namespace Genesis.Core.Content;

[JsonDerivedType(typeof(Mobile), typeDiscriminator: nameof(Mobile))]
[JsonDerivedType(typeof(Object), typeDiscriminator: nameof(Object))]
public abstract class Entity
{
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    internal virtual ICollection<Entity> Entities { get => []; init { } }

    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; }

    [JsonIgnore, NotMapped]
    public Entity? Parent { get; internal set; }

    public Dictionary<string, Dynamic> Properties
    {
        internal get => _properties.ToDictionary(x => x.Key, x => x.Value);
        init => value.ForEach(x => _properties[x.Key] = x.Value);
    }

    internal virtual void Register(Entity entity)
    {
        throw new ArgumentException("Entity Not Supported");
    }

    internal virtual void Unregister(Entity entity)
    {
        throw new ArgumentException("Entity Not Supported");
    }
}
