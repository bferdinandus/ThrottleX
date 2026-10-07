using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ThrottleX.Core;
using ThrottleX.Core.Services.SystemTime;

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
        DateTimeOffset expectedTime = new(2026, 9, 30, 20, 45, 0, TimeSpan.Zero);
        SystemTimeService service = new(
            Options.Create(new SystemTimeOptions()),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(expectedTime)
        );

        DateTimeOffset actual = service.GetHostTimeUtc();
        Assert.Equal(expectedTime, actual);
    }

    [Fact]
    public void IsTimeDifferent_WithinThreshold_ReturnsFalse()
    {
        DateTimeOffset hostTime = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        DateTimeOffset browserTime = hostTime.AddSeconds(30); // 30s difference, threshold is 60s

        SystemTimeService service = new(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        bool isDifferent = service.IsTimeDifferent(browserTime, out TimeSpan diff);
        Assert.False(isDifferent);
        Assert.Equal(TimeSpan.FromSeconds(-30), diff);
    }

    [Fact]
    public void IsTimeDifferent_ExceedingThreshold_ReturnsTrue()
    {
        DateTimeOffset hostTime = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        DateTimeOffset browserTime = hostTime.AddSeconds(120); // Host is behind browser by 120s

        SystemTimeService service = new(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        bool isDifferent = service.IsTimeDifferent(browserTime, out TimeSpan diff);
        Assert.True(isDifferent);
        Assert.Equal(TimeSpan.FromSeconds(-120), diff);
    }

    [Fact]
    public void IsTimeDifferent_HostAheadOfBrowser_ReturnsTrue()
    {
        DateTimeOffset hostTime = new(2026, 9, 30, 20, 0, 0, TimeSpan.Zero);
        DateTimeOffset browserTime = hostTime.AddMinutes(-5); // Host is 5 min ahead of browser

        SystemTimeService service = new(
            Options.Create(new SystemTimeOptions { DriftThresholdSeconds = 60 }),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(hostTime)
        );

        bool isDifferent = service.IsTimeDifferent(browserTime, out TimeSpan diff);
        Assert.True(isDifferent);
        Assert.Equal(TimeSpan.FromMinutes(5), diff);
    }

    [Fact]
    public async Task SetHostTimeAsync_HandlesSystemCommandExecution()
    {
        DateTimeOffset newTime = new(2026, 9, 30, 20, 43, 15, TimeSpan.Zero);

        SystemTimeService service = new(
            Options.Create(new SystemTimeOptions()),
            NullLogger<SystemTimeService>.Instance,
            new CustomTimeProvider(newTime)
        );

        TimeSyncResult result = await service.SetHostTimeAsync(newTime);
        Assert.NotNull(result);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Startup_ConfiguresAndResolves_SystemTimeService()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemTime:DriftThresholdSeconds"] = "120"
            })
            .Build();

        Mock<IHostEnvironment> environment = new();
        environment
            .SetupGet(e => e.EnvironmentName)
            .Returns(Environments.Development);

        ServiceCollection services = new();
        services.AddLogging();
        Startup startup = new(config, environment.Object);
        startup.ConfigureServices(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        ISystemTimeService? timeService = provider.GetService<ISystemTimeService>();

        Assert.NotNull(timeService);
        Assert.Equal(TimeSpan.FromSeconds(120), timeService.DriftThreshold);
    }
}
