namespace Genesis.Core;

public sealed class GameEngine
{
    public GameEngine(Content.Manager content, Network.Manager network)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Network = network ?? throw new ArgumentNullException(nameof(network));
    }

    public Content.Manager Content { get; }

    public Network.Manager Network { get; }

    public void Start(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Content.Start(this, cancellationToken);
    }

    public void Stop(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Content.Stop(this, cancellationToken);
    }
}
