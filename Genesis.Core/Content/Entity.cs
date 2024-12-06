using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using Genesis.Core.Entities.Attributes;
using Genesis.Core.Network;

namespace Genesis.Core.Content;

public abstract class Entity
{
    protected readonly ConcurrentDictionary<Guid, Entity> _entities = [];
    private Entity? _parent;
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    [JsonIgnore]
    internal Client? Client { get; set; }

    public Guid EntityId { get; init; } = Guid.NewGuid();

    [JsonIgnore]
    public bool IsEnabled { get; private set; }

    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public Entity? Parent
    {
        get => _parent;
        internal set => _parent = value;
    }

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

    private void DisableMembers(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        foreach (var property in GetType().GetProperties())
        {
            if (property.GetCustomAttribute<MemberAttribute>() is var attribute)
            {
                if (attribute is not null)
                {
                    (property.GetValue(this) as IEnumerable<Entity>)?.ForEach(entity => entity.Disable(engine));
                }
            }
        }
    }

    private void EnableMembers(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        foreach (var property in GetType().GetProperties())
        {
            if (property.GetCustomAttribute<MemberAttribute>() is var attribute)
            {
                if (attribute is not null)
                {
                    (property.GetValue(this) as IEnumerable<Entity>)?.ForEach(entity => entity.Enable(engine));
                }
            }
        }
    }

    protected virtual Entity? FindMember(string keyword, ref int index) => null;

    /// <summary>
    /// Unloads existing entity from game engine
    /// </summary>
    public void Destroy(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        _entities.Values.ForEach(x => x.Destroy(engine));

        Parent?.Unregister(this);

        engine.Content.Disable(this);

        engine.Content.Unregister(this);
    }

    /// <summary>
    /// Enables entity within the engine
    /// </summary>
    public void Disable(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Disable(this);

        DisableMembers(engine);

        IsEnabled = false;
    }

    /// <summary>
    /// Enables entity within the engine
    /// </summary>
    public void Enable(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Enable(this);

        EnableMembers(engine);

        IsEnabled = true;
    }

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    /// <summary>
    /// Loads new entity into game engine
    /// </summary>
    public void Load(GameEngine engine, Entity? parent = null, bool enable = false)
    {
        ArgumentNullException.ThrowIfNull(engine);

        _entities.Values.ForEach(x => x.Load(engine));

        engine.Content.Register(this);

        parent?.Register(this);

        if (enable)
        {
            Enable(engine);
        }
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
