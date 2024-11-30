using Loconet;
using Loconet.Msg;
using Loconet.Msg.Accessor;
using Shared;
using Shared.LocoTable;
using Shared.Models;
using ThrottleX.Core.LocoTable;
using static ThrottleX.Core.Loconet.SlotControl.State;

namespace ThrottleX.Core.Loconet;

public class LoconetSend : IDisposable
{
    private readonly ILoconet2Table _locoTable;
    private readonly LoconetClient _loconetClient;
    private readonly CommandStationMirror _mirror;
    private readonly Dictionary<IAddress, SlotControl> _slotControl = new ();
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly CancellationToken _cancellation;
    private readonly AutoResetEvent _wake = new (false);
    private readonly Thread _thread;
    private ILogger _logger => _loconetClient.Logger;

    public enum EState
    {
        Init,
        WaitForOperational,
        Initialize,
        NormalOperation,
        Exit,
        Exception,
    }

    public EState State { private set; get; } = EState.Init;
    public CommandStation? GuessedCommandStation { private set; get; }

    public LoconetSend(LoconetClient loconetClient, CommandStationMirror mirror, ILoconet2Table locoTable)
    {
        _loconetClient = loconetClient;
        _mirror = mirror;
        _locoTable = locoTable;
        _cancellation = _cancellationTokenSource.Token;
        loconetClient.OnConnectionEstablished += ConnectionEstablishedHandler;

        _thread = new Thread(SendThreadMain);
    }

    public void Start()
    {
        _thread.Start();
    }

    private void ConnectionEstablishedHandler()
    {
        _wake.Set();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }

    private void SendThreadMain()
    {
        var SendWait = TimeSpan.FromSeconds(1);

        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                State = EState.WaitForOperational;
                WaitForOperational();
                State = EState.Initialize;
                Initialize();
                State = EState.NormalOperation;
                NormalOperation();
            }
            catch (OperationCanceledException)
            {
                State = EState.Exit;
                return;
            }
            catch (Exception ex)
            {
                State = EState.Exception;
                _logger.LogError(ex, $"Caught exception in Loconet send thread, waiting {SendWait.TotalSeconds}s and retrying to go to normal operation...");
                _cancellation.WaitHandle.WaitOne(SendWait);
            }
        }
    }

    private void WaitForOperational()
    {
       if (_loconetClient.IsOperational)
           return; // state sais we are operational, let's go (this is for catching exceptions while staying operational)

       var result = WaitHandle.WaitAny(new[] { _cancellation.WaitHandle, _wake });
       switch (result)
       {
           case 0: throw new OperationCanceledException();  // _cancellation ends thread
           case 1: return; // _wake was triggered, let's go (this is for newly established connections)
           default: throw new InvalidOperationException(result.ToString()); // illegal value!?
       }
    }

    private void Initialize()
    {
        GuessedCommandStation = null;

        var rqSlData = new RqSlData(0);

        var success = _loconetClient.SendAndWaitReply(rqSlData, out SlRdData? reply);

        if (success == LoconetClient.LoconetSendResult.Success)
        {
            _logger.LogDebug($"Reply is {reply}");
            foreach (var tuple in CommandStation.Evaluate(reply!))
            {
                _logger.LogDebug($"{tuple.percent}% for {tuple.cs.Title}");
            }

            GuessedCommandStation = CommandStation.Guess(reply!);
            _logger.LogInformation($"Guessing this command station is {GuessedCommandStation}");
        }
        else
        {
            _logger.LogInformation($"Failed to query system slot: {success}");
        }
    }

    public void NormalOperation()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            bool found = false;

            var index = 0;

            while (index < _locoTable.Count)
            {
                if (_cancellation.IsCancellationRequested)
                    return;

                var row = _locoTable[index];
                var active = row.IsLocoActivatedForThisLoconet(_loconetClient.Index);
                var known = _slotControl.ContainsKey(row.Address);

                if (active || known)
                {
                    // Now that we either controlled it previously or want to control it now,
                    // we need to look up our state or set a state up.
                    SlotControl slot = known
                                     ? _slotControl[row.Address]
                                     : _slotControl[row.Address] = new SlotControl(row, _loconetClient);

                    switch (active, slot.SlotState)
                    {
                        case (false, None):      // stay Uninitialized
                        case (false, Inactive):  // stay Inactive
                            break;

                        case (false, Active):   // stopping to control loco
                            slot.SlotState = Deactivate(slot);
                            found = true;
                            break;

                        case (false, Failed):   // we failed to occupy but want no more
                        case (false, Occupied): // slot was already occupied when we tried, but want no more
                            slot.SlotState = Inactive;
                            found = true;
                            break;

                        case (true, None):      // slot not yet touched, activating now
                        case (true, Inactive):  // we want to activate the slot _again_
                            slot.SlotState = Activate(row, slot);
                            found = true;
                            break;

                        case (true, Active):    // stay active and operate the slot
                            found |= Operate(row, slot);
                            break;

                        case (true, Failed):    // stay failed
                        case (true, Occupied):  // stay occupied
                            break;
                    }
                }

                index++;
            }

            if (!found) // sleep a tiny moment if we did not have to do _anything_
                _cancellation.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(10));
        }
    }

    private bool Operate(ILoconet2Row row, SlotControl slot)
    {
        if (slot.IsEmergencyStopRequested(row.EmergencyStopCounter))
        {// above function aligns mirror counter and sets spd value
            slot.SendSpeed(1); // emergency stop
            return true; // TODO: do we need to delay the subsequend speed update for a moment to ensure CS sends out ESTOP to DCC?
        }

        byte requestedSpeed = row.RequestedSpeed.LocoNet;
        if (slot.LastSentSpeed.HasValue && slot.LastSentSpeed.Value != requestedSpeed)
        {
            slot.SendSpeed(requestedSpeed);
            return true;
        }

        byte requestedDirf = row.RequestedDirection == Direction.Forward ? (byte)EDirf.Dir : (byte)0;
        //TODO: add functions
        if (slot.LastSentDirf.HasValue && slot.LastSentDirf!.Value != requestedDirf)
        {
            slot.SendDirf(requestedDirf);
            return true;
        }

        return false;
    }

    private SlotControl.State Deactivate(SlotControl slot)
    {
        var request = new SlotStat1();
        request.Slot.Value = slot.SlotNumber!.Value;
        request.Stat1.Value = slot.LastKnownStat1!.Value;

        _loconetClient.BlockingSend(request); // fire and forget

        return SlotControl.State.Inactive;
    }

    private class OccupiedException : Exception { }

    private class FailedException : Exception { }

    /// <summary>
    /// We want to occupy a slot. This can either succeed and deliver data from the slot
    /// or it can fail in two ways modelled by exceptions
    /// </summary>
    /// <param name="row"></param>
    /// <param name="slot"></param>
    /// <returns>new state of slot control: can be Active, Occupied or Failed</returns>
    private SlotControl.State Activate(ILoconet2Row row, SlotControl slot)
    {
        try
        {
            var slotReply = Activating(row, slot);
            row.DeliverCommandStationState(_loconetClient.Index, slotReply);
            return Active;
        }
        catch (OccupiedException)
        {
            row.FetchingFromCommandStationOccupied(_loconetClient.Index);
            return Occupied;
        }
        catch (FailedException)
        {
            row.FetchingFromCommandStationFailed(_loconetClient.Index);
            return Failed;
        }
    }

    /// <summary>
    /// This is the process to occupy a slot. The entire way through this method is the
    /// thick line in the flow chart:
    /// https://github.com/bferdinandus/ThrottleX/wiki/LocoNet-connection#send-thread-requesting-a-slot
    /// </summary>
    /// <param name="row"></param>
    /// <param name="slot"></param>
    /// <returns></returns>
    /// <exception cref="OccupiedException">Thrown if the slot is IN_USE by a device with different ID</exception>
    /// <exception cref="FailedException">Thrown by called methods on any failure sending LocoNet messages in the course of occupying the slot</exception>"
    private OccupySlotReply Activating(ILoconet2Row row, SlotControl slot)
    {
        var slotData = GetSlotByAddress(row.Address);
        slot.InitializeFromSlot(slotData);

        if (slotData!.StatBusyActive.AsEnum == ESlotStatusBusyActive.IN_USE)
        {
            if (row.SlotId.Equals(slotData.Id))
                return new OccupySlotReply(slotData); // already IN_USE by us
            else
                throw new OccupiedException(); // slot is IN_USE by some other device
        }

        slotData = NullMove(slotData.Slot.Value);
        if (row.SlotId.Equals(slotData.Id))
            return new OccupySlotReply(slotData);

        WriteIdIntoSlot(slotData, row.SlotId);
        return new OccupySlotReply(slotData);
    }

    private SlRdData GetSlotByAddress(IAddress address)
    {
        var request = new LocoAdr();
        (request.Adr.Value, request.AdrHigh.Value) = address.Loconet;

        var result = _loconetClient.SendAndWaitReply(request, out SlRdData? slotData);

        if (result != LoconetClient.LoconetSendResult.Success)
            throw new FailedException();

        return slotData!;
    }

    private SlRdData NullMove(byte slot)
    {
        var nullMove = new MoveSlots();
        nullMove.Src.Value = slot;
        nullMove.Dest.Value = slot;

        var result = _loconetClient.SendAndWaitReply(nullMove, out SlRdData? slotData);

        if (result != LoconetClient.LoconetSendResult.Success)
            throw new FailedException();

        return slotData!;
    }

    private void WriteIdIntoSlot(SlRdData slotData, (byte id1, byte id2) slotId)
    {
        var request = new WrSlData(slotData);
        request.Id1.Value = slotId.id1;
        request.Id2.Value = slotId.id2;

        var result = _loconetClient.SendAndWaitReply(request, out LongAck? lack);

        if (result != LoconetClient.LoconetSendResult.Success)
            throw new FailedException();

        //TODO: is there a value in lack that tells us about success or failure?
    }
}
