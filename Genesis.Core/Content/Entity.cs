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
    internal bool IsEnabled { get; private set; }

    [JsonIgnore]
    internal bool IsLoaded { get; private set; }

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

    protected virtual Entity? FindMember(string keyword, ref int index) => null;

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

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    /// <summary>
    /// Enables entity within the engine
    /// </summary>
    public void Disable(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsEnabled == false)
        {
            throw new InvalidOperationException();
        }

        engine.Content.Disable(this);

        DisableMembers(engine);

        IsEnabled = true;
    }

    /// <summary>
    /// Enables entity within the engine
    /// </summary>
    public void Enable(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsEnabled == true)
        {
            throw new InvalidOperationException();
        }

        engine.Content.Enable(this);

        EnableMembers(engine);

        IsEnabled = true;
    }

    /// <summary>
    /// Loads new entity into the engine as disabled
    /// </summary>
    public void Load(GameEngine engine, Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsLoaded == true)
        {
            throw new InvalidOperationException();
        }

        _entities.Values.ForEach(x => x.Load(engine));

        engine.Content.Register(this);

        parent?.Register(this);

        IsLoaded = true;
    }

    public virtual void Register(Entity entity)
    {
        throw new ArgumentException($"Unable to register entity: {entity}");
    }

    /// <summary>
    /// Unloads existing entity from the engine as disabled
    /// </summary>
    public void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsLoaded == false)
        {
            throw new InvalidOperationException();
        }

        _entities.Values.ForEach(x => x.Unload(engine));

        engine.Content.Unregister(this);

        Parent?.Unregister(this);

        IsLoaded = false;
    }

    public virtual void Unregister(Entity entity)
    {
        throw new ArgumentException($"Unable to unregister entity: {entity}");
    }
}
