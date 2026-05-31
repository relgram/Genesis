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

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    public bool Equals(Entity? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public Entity? FindMember(string keyword, int index = 0, Func<Entity, bool>? predicate = null, Func<Entity, bool>? order = null)
    {
        return string.IsNullOrWhiteSpace(keyword) ? default : FindMember(keyword, ref index, predicate, order);
    }

    public override int GetHashCode() => HashCode.Combine(Id.GetHashCode());

    public void Load(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        driver.Content.Register(this);

        Entities.ForEach(x => x.Load(driver));
    }

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

        if (order is not null)  matches = matches.OrderByDescending(order);

        return matches.Find(x => true, ref index);
    }
}
