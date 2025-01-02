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
    private readonly HashSet<Entity> _internal = [];
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    [NotMapped]
    [JsonIgnore]
    public Client? Client { get; internal set; }

    internal ICollection<Entity> Entities
    {
        get => [.. _internal.Where(x => x is not Player)];
        init => value.ForEach(Register);
    }

    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; }

    [JsonIgnore, NotMapped]
    public Entity? Parent { get; internal set; }

    [JsonInclude]
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

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.Add(entity) == true)
        {
            entity.Parent?.Unregister(entity);

            entity.Parent = this;
        }
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_internal.Remove(entity) == true)
        {
            entity.Parent = null;
        }
    }

    public bool Equals(Entity? other)
    {
        return other != null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity);
    }

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public void Load(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Register(this);

        Entities.ForEach(x => x.Load(engine));
    }

    public virtual void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        Entities.ForEach(x => x.Unload(engine));

        engine.Content.Unregister(this);

        Parent?.Unregister(this);
    }
}
