using System.Text.Json.Serialization;
using Genesis.Core.Network;

namespace Genesis.Core.Content;

public abstract class Entity
{
    private readonly Dictionary<string, Dynamic> _properties = new(StringComparer.OrdinalIgnoreCase);

    [JsonConstructor]
    public Entity(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    [JsonIgnore]
    public Client? Client { get; internal set; }

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; }

    [JsonIgnore]
    public Entity? Parent { get; internal set; }

    [JsonInclude]
    public Dictionary<string, Dynamic> Properties
    {
        internal get => _properties.ToDictionary(x => x.Key, x => x.Value);
        init => value.ForEach(x => _properties[x.Key] = x.Value);
    }

    [JsonIgnore]
    protected HashSet<Entity> Entities { get; } = [];

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    public bool Equals(Entity? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public Entity? FindMember(string keyword, int index = 0)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index);
    }

    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Load entity into running game instance.
    /// </summary>
    public void Load(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        engine.Content.Register(this);

        LoadMembers(engine);
    }

    /// <summary>
    /// Unload entity from running game instance.
    /// </summary>
    public virtual void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        UnloadMembers(engine);

        engine.Content.Unregister(this);
    }

    internal void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (Entities.Add(entity) == true)
        {
            entity.Parent?.Unregister(entity);

            entity.Parent = this;
        }
    }

    internal void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (Entities.Remove(entity) == true)
        {
            entity.Parent = null;
        }
    }

    protected virtual Entity? FindMember(string keyword, ref int index) => null;

    protected virtual void LoadMembers(GameEngine engine)
    {
        // intentionally left blank
    }

    protected virtual void UnloadMembers(GameEngine engine)
    {
        // intentionally left blank
    }
}
