using Loconet;
using Microsoft.Extensions.Logging;
using Shared.LocoTable;
using Shared.Models;
using System.Collections.Concurrent;
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

    private readonly FunctionProcessing _functions = new();

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
            var (Result, Reply) = await _allLoconetsReply.WaitAsync(cancel);
            if (Reply.HasValue)
                InitializeFromSlot(Reply.Value);
            return (Result, Reply);
        }
        finally
        {
            _allLoconetsReply?.Dispose();
            _allLoconetsReply = null;
        }
    }

    /// <summary>
    /// Setting previous information about speed and direction from slot.
    /// We are not storing function state from slot only for web server display, 
    /// because anything that comes from the throttle is queued and propagated 
    /// to the command station without regard to any previous state. 
    /// E.g. if wiFRED forces a function to Off we will transmit this information 
    /// even if the slot information suggests that the function was Off before.
    /// </summary>
    /// <param name="value">reply from occupying the slot in the command station</param>
    private void InitializeFromSlot(OccupySlotReply value)
    {
        RequestedDirection = value.SlotDirection;
        RequestedSpeed = value.SlotSpeed;

        var functionString = _functions.InitializeFromCommandStation(value.SlotFunctions);
        _logger.LogInformation($"Initialized from command station: speed {RequestedSpeed}, {RequestedDirection}, {functionString}");
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

    void IThrottle2Row.SetFunctionKey(int number, bool newButtonState)
    {
        _functions.SetFunctionKey(number, newButtonState);
    }

    void IThrottle2Row.ForceFunction(int number, bool newFunctionState)
    {
        _functions.ForceFunction(number, newFunctionState);
    }

    void IThrottle2Row.SetMomentaryFunction(int number, bool newMomentaryConfig)
    {
        _functions.SetMomentaryFunction(number, newMomentaryConfig);
    }

    public IAddress Address { get; }

    public Speed RequestedSpeed { get; private set; } = new Speed();

    public Direction RequestedDirection { get; private set; }

    public int EmergencyStopCounter { get; private set; }

    (byte id1, byte id2) ILoconet2Row.SlotId => (42, 1);//TODO: get low 7 bits of IP address?

    /// <summary>
    /// Dequeue from _functionQueue
    /// </summary>
    bool ILoconet2Row.NextRequestedFunction(out FunctionState? functionState)
    {
        return _functions.Dequeue(out functionState);
    }

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
