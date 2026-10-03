using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ThrottleX.Core;
using ThrottleX.Core.SystemTime;

namespace Unittests.SystemTime;

public class SystemTimeServiceTest
{
    private sealed class CustomTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public void GetHostTimeUtc_ReturnsProvidedTime()
    {
        var expectedTime = new DateTimeOffset(2026, 9, 30, 20, 45, 0, TimeSpan.Zero);
        var service = new SystemTimeService(
            Options.Create(new SystemTimeOptions()),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(expectedTime)
        );

        var actual = service.GetHostTimeUtc();
        Assert.Equal(expectedTime, actual);
    }

    [Fact]
    public void IsTimeDifferent_WithinThreshold_ReturnsFalse()
    {
        var hostTime = new DateTimeOffset(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        var browserTime = hostTime.AddSeconds(30); // 30s difference, threshold is 60s

        var service = new SystemTimeService(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        var isDifferent = service.IsTimeDifferent(browserTime, out var diff);
        Assert.False(isDifferent);
        Assert.Equal(TimeSpan.FromSeconds(-30), diff);
    }

    [Fact]
    public void IsTimeDifferent_ExceedingThreshold_ReturnsTrue()
    {
        var hostTime = new DateTimeOffset(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        var browserTime = hostTime.AddSeconds(120); // Host is behind browser by 120s

        var service = new SystemTimeService(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        var isDifferent = service.IsTimeDifferent(browserTime, out var diff);
        Assert.True(isDifferent);
        Assert.Equal(TimeSpan.FromSeconds(-120), diff);
    }

    [Fact]
    public void IsTimeDifferent_HostAheadOfBrowser_ReturnsTrue()
    {
        var hostTime = new DateTimeOffset(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        var browserTime = hostTime.AddMinutes(-5); // Host is 5 min ahead of browser

        var service = new SystemTimeService(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        var isDifferent = service.IsTimeDifferent(browserTime, out var diff);
        Assert.True(isDifferent);
        Assert.Equal(TimeSpan.FromMinutes(5), diff);
    }

    [Fact]
    public async Task SetHostTimeAsync_HandlesSystemCommandExecution()
    {
        var newTime = new DateTimeOffset(2026, 9, 30, 20, 43, 15, TimeSpan.Zero);

        var service = new SystemTimeService(
            Options.Create(new SystemTimeOptions()),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(newTime)
        );

        var result = await service.SetHostTimeAsync(newTime);
        Assert.NotNull(result);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Startup_ConfiguresAndResolves_SystemTimeService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemTime:DriftThresholdSeconds"] = "120"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var startup = new Startup(config);
        startup.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var timeService = provider.GetService<ISystemTimeService>();

        Assert.NotNull(timeService);
        Assert.Equal(TimeSpan.FromSeconds(120), timeService.DriftThreshold);
    }
}
