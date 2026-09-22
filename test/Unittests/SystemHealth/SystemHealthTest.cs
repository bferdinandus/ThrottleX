using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ThrottleX.Core.SystemHealth;
using Xunit;

namespace Unittests.SystemHealth;

public class SystemHealthTest
{
    [Fact]
    public void SystemMetrics_Formatters_ProduceExpectedStrings()
    {
        var metrics = new SystemMetrics
        {
            CpuUsagePercent = 25.4,
            ProcessCpuUsagePercent = 1.5,
            ProcessCpuUsagePercent1Min = 1.2,
            ProcessCpuUsagePercent5Min = 0.8,
            ProcessCpuUsagePercent15Min = 0.5,
            MemoryUsagePercent = 58.7,
            UsedMemoryBytes = (long)(2.34 * 1024 * 1024 * 1024),
            TotalMemoryBytes = (long)(3.8 * 1024 * 1024 * 1024),
            ProcessWorkingSetBytes = 45 * 1024 * 1024,
            ProcessManagedMemoryBytes = 12 * 1024 * 1024,
            NetworkRxBytesPerSec = 15360, // 15.0 KB/s
            NetworkTxBytesPerSec = 2048,  // 2.0 KB/s
            CpuTemperatureCelsius = 43.5
        };

        Assert.Equal("25%", metrics.FormattedCpuUsage);
        Assert.Equal("1.2% / 0.8% / 0.5%", metrics.FormattedAppCpuUsage);
        Assert.Equal("1.2% / 0.8% / 0.5%", metrics.FormattedProcessCpuUsage);
        Assert.Equal("59%", metrics.FormattedMemoryUsage);
        Assert.Equal("2.3 / 3.8 GB", metrics.FormattedMemoryDetailed);
        Assert.Equal("45.0 MB", metrics.FormattedProcessMemory);
        Assert.Equal("↓ 15.0 KB/s  ↑ 2.0 KB/s", metrics.FormattedNetworkRate);
        Assert.Equal("43.5°C", metrics.FormattedCpuTemperature);
    }

    [Fact]
    public void SystemMetrics_FormatBytes_HandlesVariousMagnitudes()
    {
        Assert.Equal("0 MB", SystemMetrics.FormatBytes(0));
        Assert.Equal("500.0 KB", SystemMetrics.FormatBytes(500 * 1024));
        Assert.Equal("24.5 MB", SystemMetrics.FormatBytes((long)(24.5 * 1024 * 1024)));
        Assert.Equal("1.45 GB", SystemMetrics.FormatBytes((long)(1.45 * 1024 * 1024 * 1024)));
    }

    [Fact]
    public void SystemMetrics_FormatRate_HandlesVariousMagnitudes()
    {
        Assert.Equal("500 B/s", SystemMetrics.FormatRate(500));
        Assert.Equal("1.5 KB/s", SystemMetrics.FormatRate(1536));
        Assert.Equal("10.0 MB/s", SystemMetrics.FormatRate(10 * 1024 * 1024));
        Assert.Equal("1.2 GB/s", SystemMetrics.FormatRate(1.2 * 1024 * 1024 * 1024));
    }

    [Fact]
    public void LinuxSystemMetricsReader_ParsesCpuLoadAcrossDeltas()
    {
        var files = new Dictionary<string, string>
        {
            ["/proc/stat"] = "cpu  100 0 50 850 0 0 0 0 0 0\ncpu0 100 0 50 850 0 0 0 0 0 0\n",
            ["/proc/meminfo"] = "MemTotal: 4000000 kB\nMemAvailable: 2000000 kB\n",
            ["/proc/net/dev"] = "Inter-|   Receive | Transmit\n face |bytes ...\n  eth0: 1000 0 0 0 0 0 0 0 2000 0 0 0 0 0 0 0\n",
            ["/sys/class/thermal/thermal_zone0/temp"] = "45000\n"
        };

        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var reader = new LinuxSystemMetricsReader(
            path => files.ContainsKey(path),
            path => files.TryGetValue(path, out var content) ? content.Split('\n', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            path => files.TryGetValue(path, out var content) ? content : string.Empty,
            () => now,
            () => 67108864, // 64 MB
            () => 20971520  // 20 MB
        );

        // First sample establishes baseline
        var sample1 = reader.ReadMetrics();
        Assert.Equal(0.0, sample1.CpuUsagePercent);
        Assert.Equal(50.0, sample1.MemoryUsagePercent);
        Assert.Equal(45.0, sample1.CpuTemperatureCelsius);
        Assert.Equal(67108864, sample1.ProcessWorkingSetBytes);
        Assert.Equal(20971520, sample1.ProcessManagedMemoryBytes);
        Assert.Equal("64.0 MB", sample1.FormattedProcessMemory);

        // Second sample with active cpu load:
        // total increased by 1000 (active = 300 user+system, idle = 700) -> 30% load
        files["/proc/stat"] = "cpu  300 0 150 1550 0 0 0 0 0 0\n";
        // Net: rx increased by 10000 bytes over 2 seconds -> 5000 B/s
        files["/proc/net/dev"] = "Inter-|   Receive | Transmit\n face |bytes ...\n  eth0: 11000 0 0 0 0 0 0 0 4000 0 0 0 0 0 0 0\n";
        now = now.AddSeconds(2);

        var sample2 = reader.ReadMetrics();
        Assert.Equal(30.0, sample2.CpuUsagePercent, precision: 1);
        Assert.Equal(5000.0, sample2.NetworkRxBytesPerSec, precision: 1);
        Assert.Equal(1000.0, sample2.NetworkTxBytesPerSec, precision: 1);
    }

    [Fact]
    public void LinuxSystemMetricsReader_CalculatesMemoryFallbackWhenMemAvailableMissing()
    {
        var files = new Dictionary<string, string>
        {
            ["/proc/meminfo"] = "MemTotal: 8000000 kB\nMemFree: 1000000 kB\nBuffers: 500000 kB\nCached: 2500000 kB\nSReclaimable: 0 kB\n"
        };

        var reader = new LinuxSystemMetricsReader(
            path => files.ContainsKey(path),
            path => files.TryGetValue(path, out var content) ? content.Split('\n', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            path => files.TryGetValue(path, out var content) ? content : string.Empty,
            () => DateTime.UtcNow
        );

        var metrics = reader.ReadMetrics();
        // Total = 8,000,000 kB, Available = 1,000,000 + 500,000 + 2,500,000 = 4,000,000 kB -> Used = 4,000,000 kB (50%)
        Assert.Equal(50.0, metrics.MemoryUsagePercent, precision: 1);
    }

    [Fact]
    public void LinuxSystemMetricsReader_IgnoresLoopbackInterface()
    {
        var files = new Dictionary<string, string>
        {
            ["/proc/net/dev"] = "Inter-|   Receive | Transmit\n face |bytes ...\n  lo: 9999999 0 0 0 0 0 0 0 9999999 0 0 0 0 0 0 0\n  wlan0: 10000 0 0 0 0 0 0 0 5000 0 0 0 0 0 0 0\n"
        };

        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var reader = new LinuxSystemMetricsReader(
            path => files.ContainsKey(path),
            path => files.TryGetValue(path, out var content) ? content.Split('\n', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            path => files.TryGetValue(path, out var content) ? content : string.Empty,
            () => now
        );

        reader.ReadMetrics();

        files["/proc/net/dev"] = "Inter-|   Receive | Transmit\n face |bytes ...\n  lo: 99999999 0 0 0 0 0 0 0 99999999 0 0 0 0 0 0 0\n  wlan0: 20000 0 0 0 0 0 0 0 7000 0 0 0 0 0 0 0\n";
        now = now.AddSeconds(1);

        var sample = reader.ReadMetrics();
        Assert.Equal(10000.0, sample.NetworkRxBytesPerSec, precision: 1);
        Assert.Equal(2000.0, sample.NetworkTxBytesPerSec, precision: 1);
    }

    [Fact]
    public void SystemHealthService_InitializesAndUpdatesMetrics()
    {
        var testMetrics = new SystemMetrics
        {
            CpuUsagePercent = 12.0,
            MemoryUsagePercent = 34.0,
            CpuTemperatureCelsius = 40.0
        };

        var mockReader = new MockMetricsReader(testMetrics);
        var options = Options.Create(new SystemHealthOptions { RefreshIntervalSeconds = 1 });
        var service = new SystemHealthService(mockReader, options, NullLogger<SystemHealthService>.Instance);

        Assert.Equal(12.0, service.CurrentMetrics.CpuUsagePercent);

        bool eventFired = false;
        service.OnMetricsUpdated += () => eventFired = true;

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        Thread.Sleep(150);
        cts.Cancel();

        Assert.True(eventFired);
    }

    [Fact]
    public void SystemHealthOptions_DefaultsToTwoSeconds()
    {
        var options = new SystemHealthOptions();
        Assert.Equal(2, options.RefreshIntervalSeconds);
    }

    [Fact]
    public void SystemHealthService_InjectsOptionsRefreshInterval()
    {
        var mockReader = new MockMetricsReader(new SystemMetrics());
        var options = Options.Create(new SystemHealthOptions { RefreshIntervalSeconds = 5 });
        var service = new SystemHealthService(mockReader, options, NullLogger<SystemHealthService>.Instance);

        Assert.Equal(TimeSpan.FromSeconds(5), service.RefreshInterval);
    }

    [Fact]
    public void SystemHealthService_ResolvesViaDependencyInjectionWithConfig()
    {
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["SystemHealth:RefreshIntervalSeconds"] = "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<SystemHealthOptions>(configuration.GetSection("SystemHealth"));
        services.AddSingleton<ISystemMetricsReader>(new MockMetricsReader(new SystemMetrics()));
        services.AddSingleton<SystemHealthService>();
        services.AddSingleton<ISystemHealthService>(sp => sp.GetRequiredService<SystemHealthService>());

        using var serviceProvider = services.BuildServiceProvider();
        var healthService = serviceProvider.GetRequiredService<SystemHealthService>();
        var interfaceService = serviceProvider.GetRequiredService<ISystemHealthService>();

        Assert.NotNull(healthService);
        Assert.Same(healthService, interfaceService);
        Assert.Equal(TimeSpan.FromSeconds(10), healthService.RefreshInterval);
    }

    [Fact]
    public void LinuxSystemMetricsReader_HandlesMissingAndMalformedFilesGracefully()
    {
        var reader = new LinuxSystemMetricsReader(
            path => false,
            path => Array.Empty<string>(),
            path => string.Empty,
            () => DateTime.UtcNow
        );

        var metrics = reader.ReadMetrics();
        Assert.NotNull(metrics);
        Assert.Null(metrics.CpuTemperatureCelsius);
        Assert.Equal(0.0, metrics.NetworkRxBytesPerSec);
        Assert.Equal(0.0, metrics.NetworkTxBytesPerSec);
    }

    [Fact]
    public void LinuxSystemMetricsReader_ReadsHwmonTemperatureFallback()
    {
        var files = new Dictionary<string, string>
        {
            ["/sys/class/hwmon/hwmon0/temp1_input"] = "52340\n"
        };

        var reader = new LinuxSystemMetricsReader(
            path => files.ContainsKey(path),
            path => files.TryGetValue(path, out var content) ? content.Split('\n', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            path => files.TryGetValue(path, out var content) ? content : string.Empty,
            () => DateTime.UtcNow
        );

        var metrics = reader.ReadMetrics();
        Assert.NotNull(metrics.CpuTemperatureCelsius);
        Assert.Equal(52.34, metrics.CpuTemperatureCelsius.Value, precision: 2);
    }

    [Fact]
    public void LinuxSystemMetricsReader_CalculatesProcessCpuUsageAndAveragesAcrossWindows()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var cpuTime = TimeSpan.Zero;
        int processorCount = 2;

        var reader = new LinuxSystemMetricsReader(
            path => false,
            path => Array.Empty<string>(),
            path => string.Empty,
            () => now,
            () => 50 * 1024 * 1024,
            () => 15 * 1024 * 1024,
            () => cpuTime,
            () => processorCount
        );

        // Sample 1: Baseline sample at t = 0s
        var sample1 = reader.ReadMetrics();
        Assert.Equal(0.0, sample1.ProcessCpuUsagePercent);
        Assert.Equal(0.0, sample1.ProcessCpuUsagePercent1Min);
        Assert.Equal(0.0, sample1.ProcessCpuUsagePercent5Min);
        Assert.Equal(0.0, sample1.ProcessCpuUsagePercent15Min);
        Assert.Equal("0.0% / 0.0% / 0.0%", sample1.FormattedAppCpuUsage);

        // Sample 2: At t = 30s, 6s of CPU time used across 2 cores -> 6 / (30 * 2) = 10%
        now = now.AddSeconds(30);
        cpuTime = TimeSpan.FromSeconds(6);

        var sample2 = reader.ReadMetrics();
        Assert.Equal(10.0, sample2.ProcessCpuUsagePercent, precision: 1);
        Assert.Equal(10.0, sample2.ProcessCpuUsagePercent1Min, precision: 1);
        Assert.Equal(10.0, sample2.ProcessCpuUsagePercent5Min, precision: 1);
        Assert.Equal(10.0, sample2.ProcessCpuUsagePercent15Min, precision: 1);
        Assert.Equal("10.0% / 10.0% / 10.0%", sample2.FormattedAppCpuUsage);

        // Sample 3: At t = 60s, additional 12s of CPU time used (total 18s)
        // Interval (30s..60s): 12 / (30 * 2) = 20% instantaneous
        // 1m window (0s..60s): 18 / (60 * 2) = 15%
        now = now.AddSeconds(30);
        cpuTime = TimeSpan.FromSeconds(18);

        var sample3 = reader.ReadMetrics();
        Assert.Equal(20.0, sample3.ProcessCpuUsagePercent, precision: 1);
        Assert.Equal(15.0, sample3.ProcessCpuUsagePercent1Min, precision: 1);
        Assert.Equal(15.0, sample3.ProcessCpuUsagePercent5Min, precision: 1);
        Assert.Equal(15.0, sample3.ProcessCpuUsagePercent15Min, precision: 1);
        Assert.Equal("15.0% / 15.0% / 15.0%", sample3.FormattedAppCpuUsage);

        // Sample 4: At t = 90s, additional 6s of CPU time used (total 24s)
        // Interval (60s..90s): 6 / (30 * 2) = 10% instantaneous
        // 1m window (cutoff = 30s, base sample at 30s has 6s cpu): (24 - 6) / ((90 - 30) * 2) = 18 / 120 = 15%
        // 5m window (cutoff = -210s, base sample at 0s has 0s cpu): 24 / (90 * 2) = 24 / 180 = 13.33%
        now = now.AddSeconds(30);
        cpuTime = TimeSpan.FromSeconds(24);

        var sample4 = reader.ReadMetrics();
        Assert.Equal(10.0, sample4.ProcessCpuUsagePercent, precision: 1);
        Assert.Equal(15.0, sample4.ProcessCpuUsagePercent1Min, precision: 1);
        Assert.Equal(13.3, sample4.ProcessCpuUsagePercent5Min, precision: 1);
        Assert.Equal(13.3, sample4.ProcessCpuUsagePercent15Min, precision: 1);
        Assert.Equal("15.0% / 13.3% / 13.3%", sample4.FormattedAppCpuUsage);
    }

    [Fact]
    public void LinuxSystemMetricsReader_ResetsCpuHistoryOnTimeOrCounterRewind()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var cpuTime = TimeSpan.FromSeconds(10);

        var reader = new LinuxSystemMetricsReader(
            path => false,
            path => Array.Empty<string>(),
            path => string.Empty,
            () => now,
            () => 50 * 1024 * 1024,
            () => 15 * 1024 * 1024,
            () => cpuTime,
            () => 1
        );

        reader.ReadMetrics();

        // Time jumps backwards
        now = now.AddSeconds(-10);
        var resetSample = reader.ReadMetrics();

        Assert.Equal(0.0, resetSample.ProcessCpuUsagePercent);
        Assert.Equal(0.0, resetSample.ProcessCpuUsagePercent1Min);
    }

    private class MockMetricsReader : ISystemMetricsReader
    {
        private readonly SystemMetrics _metrics;

        public MockMetricsReader(SystemMetrics metrics)
        {
            _metrics = metrics;
        }

        public SystemMetrics ReadMetrics() => _metrics;
    }
}
