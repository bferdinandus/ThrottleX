namespace ThrottleX.Core.Services.SystemHealth;

public interface ISystemHealthService
{
    SystemMetrics CurrentMetrics { get; }
    event Action? OnMetricsUpdated;
}
