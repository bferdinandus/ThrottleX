namespace ThrottleX.Core.Services.SystemTime;

public interface ISystemTimeService
{
    /// <summary>
    /// Gets the current host time in UTC.
    /// </summary>
    DateTimeOffset GetHostTimeUtc();

    /// <summary>
    /// Sets the host system time to the given UTC time.
    /// </summary>
    Task<TimeSyncResult> SetHostTimeAsync(DateTimeOffset newTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the provided browser time differs from the host time beyond the configured drift threshold.
    /// </summary>
    bool IsTimeDifferent(DateTimeOffset browserTimeUtc, out TimeSpan difference);

    /// <summary>
    /// The threshold duration before the time difference is considered out of sync.
    /// </summary>
    TimeSpan DriftThreshold { get; }
}
