using System.ComponentModel.DataAnnotations.Schema;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Area))]
public sealed class Area : Entity
{
    public Area(string name) : base(name)
    {
    }

    [NotMapped]
    public Room[] Rooms
    {
        get => [.. _entities.Values.OfType<Room>()];
        init => value.ForEach(Register);
    }

    protected override void LoadMembers(GameEngine engine)
    {
        var rooms = engine.Content.Query<Room>(x => x.ParentId == EntityId);
        Parallel.ForEach(rooms, room => room.Load(engine, this));
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Room room)
        {
            if (_entities.TryAdd(room.EntityId, room) is true)
            {
                room.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Room room)
        {
            if (_entities.TryRemove(room.EntityId, out var _) is true)
            {
                room.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
