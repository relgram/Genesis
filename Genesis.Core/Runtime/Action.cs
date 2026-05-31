using Genesis.Core.Entities;

namespace Genesis.Core.Runtime;

public abstract class Action
{
    public virtual string? Alias { get; }

    public abstract string Name { get; }

    public abstract bool TryExecute(Driver driver, Player sender, string message);
}
