using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Genesis.Core.Entities;
using Genesis.Core.Network;
using Object = Genesis.Core.Entities.Object;

namespace Genesis.Core.Content;

[JsonDerivedType(typeof(Effect), typeDiscriminator: nameof(Effect))]
[JsonDerivedType(typeof(Mobile), typeDiscriminator: nameof(Mobile))]
[JsonDerivedType(typeof(Object), typeDiscriminator: nameof(Object))]
public abstract class Entity
{
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    [NotMapped]
    [JsonIgnore]
    public Client? Client { get; internal set; }

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

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    protected virtual Entity? FindMember(string keyword, ref int index) => null;

    protected virtual void LoadMembers(GameEngine engine)
    {
    }

    protected virtual void UnloadMembers(GameEngine engine)
    {
    }

    internal virtual void Unregister(Entity entity)
    {
        throw new NotSupportedException("Entity Not Supported");
    }

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    public void Load(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Register(this);

        LoadMembers(engine);
    }

    public void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        UnloadMembers(engine);

        engine.Content.Unregister(this);
    }
}
