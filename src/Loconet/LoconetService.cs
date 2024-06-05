using Microsoft.Extensions.Hosting;

namespace Loconet;

public class LoconetService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
