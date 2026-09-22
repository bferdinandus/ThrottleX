using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ThrottleX.Core.SystemHealth;

public class SystemHealthService : BackgroundService, ISystemHealthService
{
    private readonly ISystemMetricsReader _metricsReader;
    private readonly ILogger<SystemHealthService>? _logger;

    public SystemMetrics CurrentMetrics { get; private set; } = new();
    public TimeSpan RefreshInterval { get; }

    public event Action? OnMetricsUpdated;

    public SystemHealthService( ISystemMetricsReader metricsReader, IOptions<SystemHealthOptions> options, ILogger<SystemHealthService> logger)
    {
        _metricsReader = metricsReader ?? throw new ArgumentNullException(nameof(metricsReader));
        _logger = logger;
        RefreshInterval = TimeSpan.FromSeconds(options.Value.RefreshIntervalSeconds);

        try
        {
            CurrentMetrics = _metricsReader.ReadMetrics();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Initial system metrics read failed.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CurrentMetrics = _metricsReader.ReadMetrics();
                OnMetricsUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing system health metrics.");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
