namespace ThrottleX.Core.SystemHealth;

public record SystemMetrics
{
    public double CpuUsagePercent { get; init; }
    public double ProcessCpuUsagePercent { get; init; }
    public double ProcessCpuUsagePercent1Min { get; init; }
    public double ProcessCpuUsagePercent5Min { get; init; }
    public double ProcessCpuUsagePercent15Min { get; init; }
    public double MemoryUsagePercent { get; init; }
    public long UsedMemoryBytes { get; init; }
    public long TotalMemoryBytes { get; init; }
    public long ProcessWorkingSetBytes { get; init; }
    public long ProcessManagedMemoryBytes { get; init; }
    public double NetworkRxBytesPerSec { get; init; }
    public double NetworkTxBytesPerSec { get; init; }
    public double? CpuTemperatureCelsius { get; init; }

    public string FormattedCpuUsage => $"{Math.Clamp(CpuUsagePercent, 0.0, 100.0):F0}%";

    public string FormattedAppCpuUsage =>
        $"{Math.Clamp(ProcessCpuUsagePercent1Min, 0.0, 100.0):F1}% / {Math.Clamp(ProcessCpuUsagePercent5Min, 0.0, 100.0):F1}% / {Math.Clamp(ProcessCpuUsagePercent15Min, 0.0, 100.0):F1}%";

    public string FormattedProcessCpuUsage => FormattedAppCpuUsage;

    public string FormattedMemoryUsage => $"{Math.Clamp(MemoryUsagePercent, 0.0, 100.0):F0}%";

    public string FormattedProcessMemory => FormatBytes(ProcessWorkingSetBytes);

    public string FormattedMemoryDetailed
    {
        get
        {
            if (TotalMemoryBytes <= 0)
                return "0 MB";

            if (TotalMemoryBytes >= 1024L * 1024L * 1024L)
            {
                var usedGb = UsedMemoryBytes / (1024.0 * 1024.0 * 1024.0);
                var totalGb = TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0);
                return $"{usedGb:F1} / {totalGb:F1} GB";
            }
            else
            {
                var usedMb = UsedMemoryBytes / (1024.0 * 1024.0);
                var totalMb = TotalMemoryBytes / (1024.0 * 1024.0);
                return $"{usedMb:F0} / {totalMb:F0} MB";
            }
        }
    }

    public string FormattedNetworkRate
    {
        get
        {
            var rx = FormatRate(NetworkRxBytesPerSec);
            var tx = FormatRate(NetworkTxBytesPerSec);
            return $"↓ {rx}  ↑ {tx}";
        }
    }

    public string FormattedCpuTemperature =>
        CpuTemperatureCelsius.HasValue ? $"{CpuTemperatureCelsius.Value:F1}°C" : string.Empty;

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "0 MB";

        if (bytes < 1024L * 1024L)
            return $"{bytes / 1024.0:F1} KB";

        if (bytes < 1024L * 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    public static string FormatRate(double bytesPerSec)
    {
        if (bytesPerSec < 0)
            bytesPerSec = 0;

        if (bytesPerSec < 1024.0)
            return $"{bytesPerSec:F0} B/s";

        if (bytesPerSec < 1024.0 * 1024.0)
            return $"{bytesPerSec / 1024.0:F1} KB/s";

        if (bytesPerSec < 1024.0 * 1024.0 * 1024.0)
            return $"{bytesPerSec / (1024.0 * 1024.0):F1} MB/s";

        return $"{bytesPerSec / (1024.0 * 1024.0 * 1024.0):F1} GB/s";
    }
}
