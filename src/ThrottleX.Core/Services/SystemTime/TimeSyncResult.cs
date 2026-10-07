namespace ThrottleX.Core.Services.SystemTime;

public record TimeSyncResult(bool Success, string? Message = null, DateTimeOffset? UpdatedTime = null)
{
    public static TimeSyncResult Succeeded(DateTimeOffset updatedTime, string? message = null) =>
        new(true, message ?? "System time updated successfully.", updatedTime);

    public static TimeSyncResult Failed(string message) =>
        new(false, message);
}
