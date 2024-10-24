using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [NotMapped]
    internal Client? Client { get; set; }

    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid EntityId { get; private set; } = Guid.NewGuid();

    [NotMapped]
    internal bool IsLoaded { get; private set; }

    public string Name { get; set; } = string.Empty;

    [NotMapped]
    public Entity? Parent
    {
        get => _parent;
        internal set
        {
            if (value is not null)
            {
                ParentId = value.EntityId;
            }

            _parent = value;
        }
    }

    public Guid ParentId { get; private set; } = Guid.Empty;

    [NotMapped]
    public Dictionary<string, Dynamic> Properties
    {
        internal get => _properties.ToDictionary(x => x.Key, x => x.Value);
        init => value.ForEach(x => _properties[x.Key] = x.Value);
    }

    protected virtual void LoadMembers(GameEngine engine)
    {

    }

    protected virtual void UnloadMembers(GameEngine engine)
    {

    }

    public Dynamic this[string key]
    {
        get => _properties.GetValueOrDefault(key, Dynamic.Empty);
        set => _properties[key] = value ?? Dynamic.Empty;
    }

    public void Load(GameEngine engine, Entity? parent)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsLoaded is true)
        {
            throw new InvalidOperationException();
        }

        engine.Content.Register(this);

        parent?.Register(this);

        if (_entities.IsEmpty is false)
        {
            foreach (var entity in _entities.Values)
            {
                entity.Load(engine, null);
            }
        }
        else
        {
            LoadMembers(engine);
        }

        IsLoaded = true;
    }

    public virtual void Register(Entity entity)
    {
        throw new ArgumentException($"Unable to register entity: {entity}");
    }

    public void Save(GameEngine engine, bool cascade)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (cascade is true)
        {
            Parallel.ForEach(_entities.Values, entity =>
            {
                entity.Save(engine, true);
            });
        }

        engine.Content.Save(this);

        IsLoaded = true;
    }

    public void Unload(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (IsLoaded is false)
        {
            throw new InvalidOperationException();
        }

        engine.Content.Unregister(this);

        Parent?.Unregister(this);

        IsLoaded = false;

        Client = null;
    }

    public virtual void Unregister(Entity entity)
    {
        throw new ArgumentException($"Unable to unregister entity: {entity}");
    }
}
