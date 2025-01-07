using System.Threading;

namespace Genesis.Core;

public sealed class Engine
{
    public Engine(Content.Manager content, Network.Manager network, Runtime.Manager runtime)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Network = network ?? throw new ArgumentNullException(nameof(network));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public Content.Manager Content { get; }

    public Network.Manager Network { get; }

    public Runtime.Manager Runtime { get; }

    public void Start(CancellationToken cancellationToken)
    {
        Runtime.Start(this, cancellationToken);
        Content.Start(this, cancellationToken);
        Network.Start(this, cancellationToken);
    }

    public void Stop(CancellationToken cancellationToken)
    {
        Network.Stop(this, cancellationToken);
        Content.Stop(this, cancellationToken);
        Runtime.Stop(this, cancellationToken);
    }
}
