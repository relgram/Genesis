using Genesis.Core;
using Microsoft.Extensions.Hosting;

namespace Genesis;

internal class Service : IHostedService
{
    private readonly Driver _driver;

    public Service(Driver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _driver.Start(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _driver.Stop(cancellationToken);
        return Task.CompletedTask;
    }
}
