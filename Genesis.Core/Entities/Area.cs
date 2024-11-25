using Genesis.Core.Content;
using Genesis.Core.Entities.Attributes;

namespace Genesis.Core.Entities;

public sealed class Area : Entity
{
    public Area(string name) : base(name)
    {
    }

    [Member]
    public ICollection<Room> Rooms
    {
        get => [.. _entities.Values.OfType<Room>()];
        init => value.ForEach(Register);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Room room)
        {
            if (_entities.TryAdd(room.EntityId, room) == true)
            {
                room.Parent?.Unregister(room);
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
            if (_entities.TryRemove(room.EntityId, out var _) == true)
            {
                room.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
