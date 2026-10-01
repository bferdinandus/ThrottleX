using System.Globalization;
using Microsoft.JSInterop;

namespace ThrottleX.Core.Shared;

public partial class TimeSyncAlert
{
    private bool _isTimeDifferent;
    private DateTimeOffset _hostTime;
    private DateTimeOffset _browserTime;
    private TimeSpan _timeDifference;
    private bool _isUpdating;
    private string? _statusMessage;
    private bool _isSuccessMessage;
    private bool _isDismissed;
    private bool _disposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CheckTimeAsync();
        }
    }

    private async Task CheckTimeAsync()
    {
        try
        {
            Logger.LogInformation("Evaluating host vs browser time synchronization...");
            var browserIsoString = await JsRuntime.InvokeAsync<string>("throttlexTime.getBrowserTimeIso");

            if (DateTimeOffset.TryParse(browserIsoString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var browserTime))
            {
                _browserTime = browserTime;
                _hostTime = TimeService.GetHostTimeUtc();
                _isTimeDifferent = TimeService.IsTimeDifferent(_browserTime, out _timeDifference);

                if (_isTimeDifferent)
                {
                    Logger.LogWarning(
                        "Time drift detected! Host UTC: {HostTime:O}, Browser UTC: {BrowserTime:O}, Difference: {TimeDifference}, Alert displayed: true",
                        _hostTime, _browserTime, _timeDifference);
                }
                else
                {
                    Logger.LogInformation(
                        "Time synchronization check passed. Host UTC: {HostTime:O}, Browser UTC: {BrowserTime:O}, Difference: {TimeDifference}",
                        _hostTime, _browserTime, _timeDifference);
                }

                StateHasChanged();
            }
            else
            {
                Logger.LogWarning("Failed to parse browser time string received via JS interop: '{BrowserTimeString}'", browserIsoString);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check time synchronization between host and browser.");
        }
    }

    private async Task SyncTimeAsync()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        _statusMessage = null;
        StateHasChanged();

        try
        {
            // Get most accurate current browser time at moment of button click
            var browserIsoString = await JsRuntime.InvokeAsync<string>("throttlexTime.getBrowserTimeIso");
            if (!DateTimeOffset.TryParse(browserIsoString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var browserTime))
            {
                browserTime = DateTimeOffset.UtcNow;
            }

            var result = await TimeService.SetHostTimeAsync(browserTime);
            if (result.Success)
            {
                _isSuccessMessage = true;
                _statusMessage = result.Message ?? "Host time has been synchronized with browser time.";
                _isTimeDifferent = false;
                _isDismissed = false;

                // Auto-clear success message after 6 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(6000);
                    if (!_disposed)
                    {
                        await InvokeAsync(() =>
                        {
                            _statusMessage = null;
                            StateHasChanged();
                        });
                    }
                });
            }
            else
            {
                _isSuccessMessage = false;
                _statusMessage = result.Message ?? "Failed to update host time.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while updating host time.");
            _isSuccessMessage = false;
            _statusMessage = $"Error syncing time: {ex.Message}";
        }
        finally
        {
            _isUpdating = false;
            StateHasChanged();
        }
    }

    private void Dismiss()
    {
        _isDismissed = true;
    }

    private static string FormatTimeDifference(TimeSpan diff)
    {
        var abs = diff.Duration();
        var direction = diff.TotalSeconds < 0 ? "behind" : "ahead of";

        if (abs.TotalDays >= 1)
        {
            return $"{(int)abs.TotalDays}d {abs.Hours}h {abs.Minutes}m ({direction} browser)";
        }

        if (abs.TotalHours >= 1)
        {
            return $"{abs.Hours}h {abs.Minutes}m {abs.Seconds}s ({direction} browser)";
        }

        if (abs.TotalMinutes >= 1)
        {
            return $"{abs.Minutes}m {abs.Seconds}s ({direction} browser)";
        }

        return $"{abs.Seconds}s ({direction} browser)";
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
