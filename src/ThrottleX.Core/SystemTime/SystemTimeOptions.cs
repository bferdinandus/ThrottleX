namespace ThrottleX.Core.SystemTime;

public class SystemTimeOptions
{
    /// <summary>
    /// Threshold in seconds for the difference between browser time and host time before showing a warning.
    /// Default is 60 seconds (1 minute).
    /// </summary>
    public int DriftThresholdSeconds { get; init; } = 60;
}
