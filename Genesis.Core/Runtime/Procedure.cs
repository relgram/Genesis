namespace Genesis.Core.Runtime;

public abstract class Procedure
{
    /// <summary>
    /// Gets unique name of the procedure
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Executes static member of procedure matching provided method and arguments
    /// </summary>
    public abstract bool Execute(GameEngine engine, string method, params object[] args);
}
