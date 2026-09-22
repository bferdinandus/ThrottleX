using System.Diagnostics;
using System.Globalization;

namespace ThrottleX.Core.SystemHealth;

public class LinuxSystemMetricsReader : ISystemMetricsReader
{
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string[]> _readAllLines;
    private readonly Func<string, string> _readAllText;
    private readonly Func<DateTime> _getUtcNow;
    private readonly Func<long> _getProcessWorkingSet;
    private readonly Func<long> _getProcessManagedMemory;
    private readonly Func<TimeSpan> _getProcessCpuTime;
    private readonly Func<int> _getProcessorCount;

    private readonly object _lock = new();

    private long _prevCpuTotal;
    private long _prevCpuIdle;
    private bool _hasCpuSample;

    private readonly List<(DateTime Timestamp, TimeSpan CpuTime)> _processCpuSamples = new();

    private long _prevNetRxBytes;
    private long _prevNetTxBytes;
    private DateTime _prevNetTime = DateTime.MinValue;
    private bool _hasNetSample;

    private static readonly string[] ThermalZonePaths =
    [
        "/sys/class/thermal/thermal_zone0/temp",
        "/sys/class/thermal/thermal_zone1/temp",
        "/sys/devices/virtual/thermal/thermal_zone0/temp",
        "/sys/class/hwmon/hwmon0/temp1_input",
        "/sys/class/hwmon/hwmon1/temp1_input",
        "/sys/class/hwmon/hwmon2/temp1_input"
    ];

    public LinuxSystemMetricsReader()
        : this(
            File.Exists,
            File.ReadAllLines,
            File.ReadAllText,
            () => DateTime.UtcNow,
            () => Process.GetCurrentProcess().WorkingSet64,
            () => GC.GetTotalMemory(false),
            () => Process.GetCurrentProcess().TotalProcessorTime,
            () => Environment.ProcessorCount)
    {
    }

    public LinuxSystemMetricsReader(
        Func<string, bool> fileExists,
        Func<string, string[]> readAllLines,
        Func<string, string> readAllText,
        Func<DateTime> getUtcNow,
        Func<long>? getProcessWorkingSet = null,
        Func<long>? getProcessManagedMemory = null,
        Func<TimeSpan>? getProcessCpuTime = null,
        Func<int>? getProcessorCount = null)
    {
        _fileExists = fileExists;
        _readAllLines = readAllLines;
        _readAllText = readAllText;
        _getUtcNow = getUtcNow;
        _getProcessWorkingSet = getProcessWorkingSet ?? (() =>
        {
            try { return Process.GetCurrentProcess().WorkingSet64; }
            catch { return 0; }
        });
        _getProcessManagedMemory = getProcessManagedMemory ?? (() =>
        {
            try { return GC.GetTotalMemory(false); }
            catch { return 0; }
        });
        _getProcessCpuTime = getProcessCpuTime ?? (() =>
        {
            try { return Process.GetCurrentProcess().TotalProcessorTime; }
            catch { return TimeSpan.Zero; }
        });
        _getProcessorCount = getProcessorCount ?? (() =>
        {
            try { return Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 1; }
            catch { return 1; }
        });
    }

    public SystemMetrics ReadMetrics()
    {
        lock (_lock)
        {
            var cpuPercent = ReadCpuUsage();
            var (processCpuPercent, processCpu1m, processCpu5m, processCpu15m) = ReadProcessCpuUsage();
            var (memPercent, usedMem, totalMem) = ReadMemoryUsage();
            var (rxRate, txRate) = ReadNetworkRates();
            var cpuTemp = ReadCpuTemperature();
            var processWorkingSet = _getProcessWorkingSet();
            var processManagedMem = _getProcessManagedMemory();

            return new SystemMetrics
            {
                CpuUsagePercent = cpuPercent,
                ProcessCpuUsagePercent = processCpuPercent,
                ProcessCpuUsagePercent1Min = processCpu1m,
                ProcessCpuUsagePercent5Min = processCpu5m,
                ProcessCpuUsagePercent15Min = processCpu15m,
                MemoryUsagePercent = memPercent,
                UsedMemoryBytes = usedMem,
                TotalMemoryBytes = totalMem,
                ProcessWorkingSetBytes = processWorkingSet,
                ProcessManagedMemoryBytes = processManagedMem,
                NetworkRxBytesPerSec = rxRate,
                NetworkTxBytesPerSec = txRate,
                CpuTemperatureCelsius = cpuTemp
            };
        }
    }

    private (double current, double avg1m, double avg5m, double avg15m) ReadProcessCpuUsage()
    {
        try
        {
            var now = _getUtcNow();
            var currentCpuTime = _getProcessCpuTime();
            var processorCount = Math.Max(1, _getProcessorCount());

            if (_processCpuSamples.Count == 0 || now < _processCpuSamples[^1].Timestamp || currentCpuTime < _processCpuSamples[^1].CpuTime)
            {
                _processCpuSamples.Clear();
                _processCpuSamples.Add((now, currentCpuTime));
                return (0.0, 0.0, 0.0, 0.0);
            }

            var last = _processCpuSamples[^1];
            var deltaWall = (now - last.Timestamp).TotalSeconds;
            var deltaCpu = (currentCpuTime - last.CpuTime).TotalSeconds;

            double currentPercent = 0.0;
            if (deltaWall > 0.0)
            {
                currentPercent = Math.Clamp((deltaCpu / (deltaWall * processorCount)) * 100.0, 0.0, 100.0);
            }

            var avg1m = CalculateAverageCpuUsage(now, TimeSpan.FromMinutes(1), currentCpuTime, processorCount);
            var avg5m = CalculateAverageCpuUsage(now, TimeSpan.FromMinutes(5), currentCpuTime, processorCount);
            var avg15m = CalculateAverageCpuUsage(now, TimeSpan.FromMinutes(15), currentCpuTime, processorCount);

            _processCpuSamples.Add((now, currentCpuTime));

            var pruneCutoff = now - TimeSpan.FromMinutes(16);
            while (_processCpuSamples.Count > 2 && _processCpuSamples[1].Timestamp < pruneCutoff)
            {
                _processCpuSamples.RemoveAt(0);
            }

            return (currentPercent, avg1m, avg5m, avg15m);
        }
        catch
        {
            return (0.0, 0.0, 0.0, 0.0);
        }
    }

    private double CalculateAverageCpuUsage(DateTime now, TimeSpan window, TimeSpan currentCpuTime, int processorCount)
    {
        if (_processCpuSamples.Count == 0)
            return 0.0;

        var cutoff = now - window;
        (DateTime Timestamp, TimeSpan CpuTime) baseSample = _processCpuSamples[0];
        for (int i = _processCpuSamples.Count - 1; i >= 0; i--)
        {
            if (_processCpuSamples[i].Timestamp <= cutoff)
            {
                baseSample = _processCpuSamples[i];
                break;
            }
        }

        var deltaWall = (now - baseSample.Timestamp).TotalSeconds;
        var deltaCpu = (currentCpuTime - baseSample.CpuTime).TotalSeconds;

        if (deltaWall <= 0.0)
            return 0.0;

        var percent = (deltaCpu / (deltaWall * processorCount)) * 100.0;
        return Math.Clamp(percent, 0.0, 100.0);
    }

    private double ReadCpuUsage()
    {
        try
        {
            const string statPath = "/proc/stat";
            if (!_fileExists(statPath))
            {
                return GetFallbackCpuUsage();
            }

            var lines = _readAllLines(statPath);
            foreach (var line in lines)
            {
                if (!line.StartsWith("cpu ", StringComparison.Ordinal))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    break;

                // parts[0] is "cpu"
                // 1: user, 2: nice, 3: system, 4: idle, 5: iowait, 6: irq, 7: softirq, 8: steal
                long user = parts.Length > 1 && long.TryParse(parts[1], out var u) ? u : 0;
                long nice = parts.Length > 2 && long.TryParse(parts[2], out var n) ? n : 0;
                long system = parts.Length > 3 && long.TryParse(parts[3], out var s) ? s : 0;
                long idle = parts.Length > 4 && long.TryParse(parts[4], out var id) ? id : 0;
                long iowait = parts.Length > 5 && long.TryParse(parts[5], out var io) ? io : 0;
                long irq = parts.Length > 6 && long.TryParse(parts[6], out var ir) ? ir : 0;
                long softirq = parts.Length > 7 && long.TryParse(parts[7], out var sir) ? sir : 0;
                long steal = parts.Length > 8 && long.TryParse(parts[8], out var st) ? st : 0;

                var idleTime = idle + iowait;
                var nonIdleTime = user + nice + system + irq + softirq + steal;
                var totalTime = idleTime + nonIdleTime;

                if (!_hasCpuSample)
                {
                    _prevCpuTotal = totalTime;
                    _prevCpuIdle = idleTime;
                    _hasCpuSample = true;
                    return 0.0;
                }

                var deltaTotal = totalTime - _prevCpuTotal;
                var deltaIdle = idleTime - _prevCpuIdle;

                _prevCpuTotal = totalTime;
                _prevCpuIdle = idleTime;

                if (deltaTotal > 0)
                {
                    var deltaActive = deltaTotal - deltaIdle;
                    return Math.Clamp((deltaActive / (double)deltaTotal) * 100.0, 0.0, 100.0);
                }

                return 0.0;
            }
        }
        catch
        {
            // Fallback gracefully on read failure
        }

        return GetFallbackCpuUsage();
    }

    private static double GetFallbackCpuUsage()
    {
        return 0.0;
    }

    private (double percent, long usedBytes, long totalBytes) ReadMemoryUsage()
    {
        try
        {
            const string memInfoPath = "/proc/meminfo";
            if (_fileExists(memInfoPath))
            {
                var lines = _readAllLines(memInfoPath);
                long totalKb = 0;
                long availKb = 0;
                long freeKb = 0;
                long buffersKb = 0;
                long cachedKb = 0;
                long sreclaimableKb = 0;
                bool hasAvail = false;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                    {
                        totalKb = ParseMemInfoValue(line);
                    }
                    else if (line.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase))
                    {
                        availKb = ParseMemInfoValue(line);
                        hasAvail = true;
                    }
                    else if (line.StartsWith("MemFree:", StringComparison.OrdinalIgnoreCase))
                    {
                        freeKb = ParseMemInfoValue(line);
                    }
                    else if (line.StartsWith("Buffers:", StringComparison.OrdinalIgnoreCase))
                    {
                        buffersKb = ParseMemInfoValue(line);
                    }
                    else if (line.StartsWith("Cached:", StringComparison.OrdinalIgnoreCase))
                    {
                        cachedKb = ParseMemInfoValue(line);
                    }
                    else if (line.StartsWith("SReclaimable:", StringComparison.OrdinalIgnoreCase))
                    {
                        sreclaimableKb = ParseMemInfoValue(line);
                    }
                }

                if (!hasAvail)
                {
                    availKb = freeKb + buffersKb + cachedKb + sreclaimableKb;
                }

                var totalBytes = totalKb * 1024L;
                var availBytes = availKb * 1024L;
                var usedBytes = Math.Max(0L, totalBytes - availBytes);

                var percent = totalBytes > 0
                    ? Math.Clamp((usedBytes / (double)totalBytes) * 100.0, 0.0, 100.0)
                    : 0.0;

                return (percent, usedBytes, totalBytes);
            }
        }
        catch
        {
            // Fallback gracefully
        }

        return GetFallbackMemoryUsage();
    }

    private static long ParseMemInfoValue(string line)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return 0;

        var part = line[(colonIdx + 1)..].Trim();
        var spaceIdx = part.IndexOf(' ');
        if (spaceIdx > 0)
        {
            part = part[..spaceIdx].Trim();
        }

        return long.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    private static (double percent, long usedBytes, long totalBytes) GetFallbackMemoryUsage()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var total = gcInfo.TotalAvailableMemoryBytes;
            var used = gcInfo.MemoryLoadBytes > 0 ? gcInfo.MemoryLoadBytes : Process.GetCurrentProcess().WorkingSet64;
            var percent = total > 0 ? Math.Clamp((used / (double)total) * 100.0, 0.0, 100.0) : 0.0;
            return (percent, used, total);
        }
        catch
        {
            return (0.0, 0, 0);
        }
    }

    private (double rxRate, double txRate) ReadNetworkRates()
    {
        try
        {
            const string netDevPath = "/proc/net/dev";
            if (!_fileExists(netDevPath))
            {
                return (0.0, 0.0);
            }

            var lines = _readAllLines(netDevPath);
            long totalRxBytes = 0;
            long totalTxBytes = 0;

            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex < 0)
                    continue;

                var iface = line[..colonIndex].Trim();
                if (string.Equals(iface, "lo", StringComparison.OrdinalIgnoreCase))
                    continue;

                var statsPart = line[(colonIndex + 1)..];
                var parts = statsPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 9)
                {
                    if (long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rx))
                        totalRxBytes += rx;

                    if (long.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tx))
                        totalTxBytes += tx;
                }
            }

            var now = _getUtcNow();
            if (!_hasNetSample)
            {
                _prevNetRxBytes = totalRxBytes;
                _prevNetTxBytes = totalTxBytes;
                _prevNetTime = now;
                _hasNetSample = true;
                return (0.0, 0.0);
            }

            var deltaSeconds = (now - _prevNetTime).TotalSeconds;
            _prevNetTime = now;

            double rxRate = 0.0;
            double txRate = 0.0;

            if (deltaSeconds > 0)
            {
                var deltaRx = totalRxBytes - _prevNetRxBytes;
                var deltaTx = totalTxBytes - _prevNetTxBytes;

                if (deltaRx >= 0)
                    rxRate = deltaRx / deltaSeconds;

                if (deltaTx >= 0)
                    txRate = deltaTx / deltaSeconds;
            }

            _prevNetRxBytes = totalRxBytes;
            _prevNetTxBytes = totalTxBytes;

            return (rxRate, txRate);
        }
        catch
        {
            return (0.0, 0.0);
        }
    }

    private double? ReadCpuTemperature()
    {
        foreach (var path in ThermalZonePaths)
        {
            try
            {
                if (!_fileExists(path))
                    continue;

                var text = _readAllText(path).Trim();
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                {
                    // Values like 45000 mean 45.0 °C
                    if (val > 1000)
                        return val / 1000.0;

                    if (val > 0)
                        return val;
                }
            }
            catch
            {
                // Try next path
            }
        }

        return null;
    }
}
