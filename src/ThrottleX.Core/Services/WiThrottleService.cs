namespace ThrottleX.Core.Services;

public class WiThrottleService : BackgroundService
{
    private readonly ILogger<WiThrottleService> _logger;

    public WiThrottleService(ILogger<WiThrottleService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Service is starting.");

        stoppingToken.Register(() => _logger.LogInformation("Background Service is stopping."));

        while (!stoppingToken.IsCancellationRequested) {
            _logger.LogInformation("Background Service is doing background work.");

            // Simulate some background work
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Background Service has stopped.");
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}