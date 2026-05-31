using Genesis.Core.Content;

namespace Genesis.Core.Runtime;

public abstract class Procedure
{
    public abstract string Name { get; }

    public abstract bool TryExecute(Driver driver, Entity sender, string method, object[] args);
}
