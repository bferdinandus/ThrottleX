namespace ThrottleX.Core.Services.SystemHealth;

public interface ISystemMetricsReader
{
    SystemMetrics ReadMetrics();
}
