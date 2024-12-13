using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using Genesis.Core.Entities;
using Genesis.Core.Entities.Attributes;
using Genesis.Core.Network;

namespace Genesis.Core.Content;

[NotMapped]
public abstract class Entity
{
    protected readonly ConcurrentDictionary<Guid, Entity> _entities = [];
    private Entity? _parent;
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    [NotMapped]
    [JsonIgnore]
    internal Client? Client { get; set; }

    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid EntityId { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    [NotMapped]
    [JsonIgnore]
    public Entity? Parent
    {
        get => _parent;
        internal set => _parent = value;
    }

    [NotMapped]
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

    private void LoadMembers(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        foreach (var property in GetType().GetProperties())
        {
            if (property.GetCustomAttribute<MemberAttribute>() is var attribute)
            {
                if (attribute is not null)
                {
                    (property.GetValue(this) as IEnumerable<Entity>)?.ForEach(engine.Content.Register);
                }
            }
        }
    }

    private void UnloadMembers(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        foreach (var property in GetType().GetProperties())
        {
            if (property.GetCustomAttribute<MemberAttribute>() is var attribute)
            {
                if (attribute is not null)
                {
                    (property.GetValue(this) as IEnumerable<Entity>)?.ForEach(engine.Content.Unregister);
                }
            }
        }
    }

    protected virtual Entity? FindMember(string keyword, ref int index) => null;

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    public void Load(GameEngine engine, Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Register(this);

        parent?.Register(this);

        LoadMembers(engine);
    }

    public void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (this is Player)
        {
            engine.Content.Save(this);
        }

        UnloadMembers(engine);

        Parent?.Unregister(this);

        engine.Content.Unregister(this);
    }

    public virtual void Register(Entity entity)
    {
        throw new ArgumentException($"Unable to register entity: {entity}");
    }

    public virtual void Unregister(Entity entity)
    {
        throw new ArgumentException($"Unable to unregister entity: {entity}");
    }
}
