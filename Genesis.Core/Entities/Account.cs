using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Genesis.Core.Content;

namespace Genesis.Core.Entities;

[Table(nameof(Account))]
public sealed class Account : Entity
{
    public Account(string name) : base(name)
    {
    }

    public ICollection<Player> Players
    {
        get => [.. _entities.Values.OfType<Player>()];
        init => value.ForEach(Register);
    }

    public override void Register(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Player player)
        {
            if (_entities.TryAdd(player.EntityId, player) == true)
            {
                player.Parent?.Unregister(player);
                player.Parent = this;
                return;
            }
        }

        base.Register(entity);
    }

    public void Save(GameEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        engine.Content.Save(this);
    }

    public static Account[] Search(GameEngine engine, Expression<Func<Account, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return engine.Content.Search(predicate);
    }

    public override void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is Player player)
        {
            if (_entities.TryRemove(player.EntityId) == true)
            {
                player.Parent = null;
                return;
            }
        }

        base.Unregister(entity);
    }
}
