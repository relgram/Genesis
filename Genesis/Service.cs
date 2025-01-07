using Genesis.Core;
using Microsoft.Extensions.Hosting;

namespace Genesis;

internal class Service : IHostedService
{
    private readonly Engine _engine;

    public Service(Engine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _engine.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _engine.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
