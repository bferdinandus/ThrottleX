using Loconet;
using Microsoft.Extensions.Logging;
using Shared.LocoTable;
using Shared.Models;
using ThrottleX.Core.Loconet;

namespace ThrottleX.Core.LocoTable;

public class LocoRowImpl : ILoconet2Row, IThrottle2Row
{
    private readonly ILogger _logger;
    private AllLoconetsReply? _allLoconetsReply = null;

    /// <summary>
    /// This is a bit field with one flag per loconet connection.
    /// If the bit is set, the loco is enabled for this command station,
    /// if the bit is zero, the loco is disable for this command station.
    /// For now we simply set all possible bits. But here would be where we read from configuration
    /// or even manipulate per web interface in order to control on what command station a
    /// single loco can be propergated.
    /// </summary>
    private readonly uint _LoconetEnabled = uint.MaxValue;

    public bool IsActive { get; private set; }

    public LocoRowImpl(IAddress address, ILogger logger)
    {
        Address = address;
        _logger = logger;
        IsActive = false;
        RequestedSpeed.SetStop();
        RequestedDirection = Direction.Forward;
    }

    void IThrottle2Row.Activate()
    {
        if (IsActive)
            _logger.LogWarning($"Activating again {Address}");
        IsActive = true;
    }

    async Task<(OccupySlotResult, OccupySlotReply?)> IThrottle2Row.WaitForSlotsAsync(CancellationToken cancel)
    {
        if (!IsActive)
            throw new InvalidOperationException("Must activate before waiting for slots.");

        try
        {
            _allLoconetsReply = new AllLoconetsReply(_logger, _LoconetEnabled);
            return await _allLoconetsReply.WaitAsync(cancel);
        }
        finally
        {
            _allLoconetsReply?.Dispose();
            _allLoconetsReply = null;
        }
    }

    void IThrottle2Row.Deactivate()
    {
        if (!IsActive)
            _logger.LogWarning($"Deactivating again {Address}");
        EmergencyStopCounter++;  // stop the loco, now that we don't want to control it any longer
        IsActive = false;
    }


    void IThrottle2Row.SetEmergencyStop()
    {
        EmergencyStopCounter++;
        RequestedSpeed.SetStop();
    }

    void IThrottle2Row.SetSpeed(int v)
    {
        RequestedSpeed.WiThrottle = v;
    }

    void IThrottle2Row.SetDirection(Direction dir)
    {
        RequestedDirection = dir;
    }

    void IThrottle2Row.SetFunction(int number, Shared.Models.FunctionButton state)
    {
        lock (RequestedFunctions)
        {
            string previous = RequestedFunctions.TryGetValue(number, out var previousValue)
                ? previousValue.ToString()
                : "unknown";
            _logger.LogTrace($"{Address}: changing F{number} from {previous} to {state}");
            RequestedFunctions[number] = state;
        }
    }

    public Dictionary<int, FunctionButton> RequestedFunctions { get; } = new();

    public IAddress Address { get; }

    public Speed RequestedSpeed { get; } = new Speed();

    public Direction RequestedDirection { get; private set; }

    public int EmergencyStopCounter { get; private set; }

    public (byte id1, byte id2) SlotId => (42, 1);//TODO: get low 7 bits of IP address?

    bool ILoconet2Row.IsLocoActivatedForThisLoconet(int loconetClient)
    {
        return IsActive && (_LoconetEnabled & 1 << loconetClient) != 0;
    }

    private void ForwardReply(Action<AllLoconetsReply> action, string msg)
    {
        var pending = _allLoconetsReply; // snapshotting, because this might be written to null concurrently on timeout

        if (pending == null)
        {
            _logger.LogWarning($"{msg} the slot for address {Address}, but we are not waiting for this reply (state={(IsActive ? "Active" : "Inactive")}");
        }
        else
        {
            _logger.LogDebug(msg);
            action(pending);
        }
    }

    void ILoconet2Row.DeliverCommandStationState(int loconetClient, OccupySlotReply slotReply)
    {
        ForwardReply(pending => pending.SlotSuccess(loconetClient, slotReply), 
                     $"LocoNet #{loconetClient} reports success for occupying");
    }

    void ILoconet2Row.FetchingFromCommandStationFailed(int loconetClient)
    {
        ForwardReply(pending => pending.SlotFailed(loconetClient),
                     $"LocoNet #{loconetClient} reports failure to occupy");
    }

    void ILoconet2Row.FetchingFromCommandStationOccupied(int loconetClient)
    {
        ForwardReply(pending => pending.SlotOccupied(loconetClient),
                     $"LocoNet #{loconetClient} reports that another device already occupies");
    }
}
