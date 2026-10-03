using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace ThrottleX.Core.SystemTime;

public class SystemTimeService : ISystemTimeService
{
    private readonly ILogger<SystemTimeService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _driftThreshold;

    public TimeSpan DriftThreshold => _driftThreshold;

    public SystemTimeService(IOptions<SystemTimeOptions> options, ILogger<SystemTimeService> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _driftThreshold = TimeSpan.FromSeconds(options.Value.DriftThresholdSeconds);
        _timeProvider = timeProvider;
    }

    public DateTimeOffset GetHostTimeUtc() => _timeProvider.GetUtcNow();

    public bool IsTimeDifferent(DateTimeOffset browserTimeUtc, out TimeSpan difference)
    {
        var hostTimeUtc = GetHostTimeUtc();
        difference = hostTimeUtc - browserTimeUtc;
        return Math.Abs(difference.TotalSeconds) >= _driftThreshold.TotalSeconds;
    }

    public async Task<TimeSyncResult> SetHostTimeAsync(DateTimeOffset newTimeUtc, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var utcDateTime = newTimeUtc.UtcDateTime;
                var formattedDate = utcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                _logger.LogInformation("Attempting to set system time to UTC: {FormattedDate}", formattedDate);

                var (exitCode, _, stderr) = RunCommand("date", $"-u -s \"{formattedDate}\"");
                if (exitCode == 0)
                {
                    _logger.LogInformation("System time set successfully via 'date -u -s'.");
                    TrySyncHwClock();
                    return TimeSyncResult.Succeeded(newTimeUtc, $"System time updated to {formattedDate} UTC.");
                }

                var errorMsg = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : $"Exit code {exitCode}";
                _logger.LogError("Failed setting system time on Linux: {Error}", errorMsg);
                return TimeSyncResult.Failed($"Failed to set system time: {errorMsg}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while setting system time.");
                return TimeSyncResult.Failed(ex.Message);
            }
        }, cancellationToken);
    }

    private static void TrySyncHwClock()
    {
        try
        {
            RunCommand("hwclock", "--systohc");
        }
        catch
        {
            // Ignore failure if no RTC hardware exists
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunCommand(string fileName, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }
}
