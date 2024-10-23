using Genesis.Core;
using Microsoft.Extensions.Hosting;

namespace Genesis;

internal class Service : IHostedService
{
    private readonly GameEngine _engine;

    public Service(GameEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _engine.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _engine.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
