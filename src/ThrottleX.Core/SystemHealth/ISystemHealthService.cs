namespace ThrottleX.Core.SystemHealth;

public interface ISystemHealthService
{
    SystemMetrics CurrentMetrics { get; }
    event Action? OnMetricsUpdated;
}
