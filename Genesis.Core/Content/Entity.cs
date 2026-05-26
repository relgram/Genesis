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
        get => new(_properties);
        init => value.ForEach(x => _properties[x.Key] = x.Value);
    }

    [JsonIgnore]
    protected HashSet<Entity> Entities { get; } = [];

    /// <summary>
    /// Gets or sets the property associated with the given key.
    /// </summary>
    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    /// <summary>
    /// Determines whether the specified Entity is equal to the current Entity.
    /// </summary>
    public bool Equals(Entity? other) => other is not null && Id == other.Id;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as Entity);

    /// <summary>
    /// Locate registered entity with specified keyword, optional filtering predicate and optional ordering.
    /// </summary>
    public Entity? FindMember(string keyword, int index = 0, Func<Entity, bool>? predicate = null, Func<Entity, bool>? order = null)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index, predicate, order);
    }

    /// <summary>
    /// Retursn the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Id.GetHashCode());

    /// <summary>
    /// Load entity into running game instance.
    /// </summary>
    public void Load(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        driver.Content.Register(this);

        Entities.ForEach(x => x.Load(driver));
    }

    /// <summary>
    /// Unload entity from running game instance.
    /// </summary>
    public virtual void Unload(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        Entities.ForEach(x => x.Unload(driver));

        driver.Content.Unregister(this);

        Parent?.Unregister(this);
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

    private Entity? FindMember(string keyword, ref int index, Func<Entity, bool>? predicate = null, Func<Entity, bool>? order = null)
    {
        var matches = Entities.Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) && (predicate is null || predicate(x)));

        if (order is not null)
        {
            matches = matches.OrderByDescending(order);
        }

        return matches.Find(x => true, ref index);
    }
}
