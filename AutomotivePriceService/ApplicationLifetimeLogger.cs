using System.Reflection;

namespace AutomotivePriceService;

public sealed class ApplicationLifetimeLogger(ILogger<ApplicationLifetimeLogger> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting service {ServiceName}", GetServiceName());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping service {ServiceName}", GetServiceName());

        return Task.CompletedTask;
    }

    private static string GetServiceName()
    {
        return Assembly.GetEntryAssembly()?.FullName ?? nameof(AutomotivePriceService);
    }
}
