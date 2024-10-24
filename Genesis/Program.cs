using Genesis.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Genesis;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureLogging(configure =>
        {
            configure.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[yyyy-MM-ddTHH:mm:ss.fffZ] ";
                options.UseUtcTimestamp = true;
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddHostedService<Service>().AddGenesis();
        });

        var host = builder.Build();

        host.Run();
    }
}
