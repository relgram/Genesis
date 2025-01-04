using Genesis.Core.Content;

namespace Genesis.Core.Runtime;

public abstract class Action
{
    /// <summary>
    /// Gets unique alias of the action.
    /// </summary>
    public virtual string? Alias { get; }

    /// <summary>
    /// Gets unique name of the action.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Executes static member of action matching parsed arguments from message.
    /// </summary>
    public abstract bool Execute(GameEngine engine, Entity sender, string message);
}
