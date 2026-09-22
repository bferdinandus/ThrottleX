namespace ThrottleX.Core.SystemHealth;

public interface ISystemMetricsReader
{
    SystemMetrics ReadMetrics();
}
