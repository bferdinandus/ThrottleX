namespace ThrottleX.Core.LocoTable;

using global::Loconet;
using ThrottleX.Core.Loconet;
using Microsoft.Extensions.Logging;
using Shared.LocoTable;
using static Shared.LocoTable.OccupySlotResult;

public sealed class AllLoconetsReply : IDisposable
{
    private readonly ILogger _logger;
    private readonly OccupySlotResult[] _results;
    private readonly OccupySlotReply?[] _slots;
    private readonly TaskCompletionSource<(OccupySlotResult result, OccupySlotReply? slot)> _tcs = new();

    public AllLoconetsReply(ILogger logger, uint usedLoconets)
    {
        _logger = logger;

        var num = LoconetService.Instance!.ClientCount;
        _results = new OccupySlotResult[num];
        _slots = new OccupySlotReply?[num];

        for (var i=0; i<_results.Length; i++)
        {
            if ((usedLoconets & 1<<i)!=0)
                _results[i] = Requesting;
            else
                _results[i] = NotRequesting;
            _slots[i] = null;
        }
    }

    private void Log()
    {
        _logger.LogDebug($"Waiting for reply: " + string.Join(", ", _results));
    }

    public async Task<(OccupySlotResult result, OccupySlotReply? slot)> WaitAsync(CancellationToken cancel)
    {
        try
        {
            return await _tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cancel).ConfigureAwait(false);
        }
        catch (Exception ex) // includes TimeoutException
        //    (OperationCanceledException) // M$ docu sais this
        //    (TaskCanceledException) // from here: https://andrewlock.net/a-deep-dive-into-the-new-task-waitasync-api-in-dotnet-6/
        {
            _logger.LogWarning(ex, "AllLoconetReply caught exception");
            return (Failure, null);
        }
    }

    private void Check()
    {
        lock (this) // serialize access to _accumulatedLoconetsReplyResult
        {
            bool failed = false;
            bool occupied = false;
            bool success = false;
            OccupySlotReply? slot = null;

            for (var i=0; i<_slots.Length; i++)
            {
                switch (_results[i])
                {
                    case Success:  success  = true; slot = _slots[i]; break;
                    case Failure:  failed   = true;                   break;
                    case Occupied: occupied = true;                   break;
                    default:              return; // not finished yet
                }
            }

            if (failed)
                _tcs.SetResult((Failure, null));
            else if (occupied)
                _tcs.SetResult((Occupied, null));
            else if (success)
                _tcs.SetResult((Success, slot));
            else
                _logger.LogError("No result found!?!?");
        }
    }

    public void SlotSuccess(int loconetClient, OccupySlotReply slotReply)
    {
        _results[loconetClient] = Success;
        _slots[loconetClient] = slotReply;
    }

    public void SlotFailed(int loconetClient)
    {
        _results[loconetClient] = Failure;
    }

    public void SlotOccupied(int loconetClient)
    {
        _results[loconetClient] = Occupied;
        Check();
    }

    public void Dispose()
    {// apparently I did not need any objects that would have been disposed here!?
    }
}
