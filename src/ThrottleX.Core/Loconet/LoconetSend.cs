using Loconet;
using Loconet.Msg;
using Loconet.Msg.Accessor;
using Shared.LocoTable;
using Shared.Models;

namespace ThrottleX.Core.Loconet;

public class LoconetSend : IDisposable
{
    private readonly LoconetClient _loconetClient;
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly CancellationToken _cancellation;
    private readonly AutoResetEvent _wake = new (false);
    private readonly ILogger _logger;
    private readonly Thread _thread;

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

    public LoconetSend(LoconetClient loconetClient)
    {
        _loconetClient = loconetClient;
        _logger = _loconetClient.Logger;
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
        for(;;)
        {
            bool found = false;

            var index = 0;

            while (index < LocoTableImpl.Instance.Count)
            {
                if (_cancellation.IsCancellationRequested)
                    return;

                var row = LocoTableImpl.Instance[index];

                switch (row.LocoRowState)
                {
                    case ELocoRowState.Requesting:
                        if (!RunRequesting(row))
                            row.FetchingFromCommandStationFailed();
                        break;
                    case ELocoRowState.Operational:
                    case ELocoRowState.Inactive:
                        break;
                }

                index++;
            }

            if (!found) // sleep a tiny moment if we did not have to do _anything_
                _cancellation.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(10));
        }
    }
    public bool RunRequesting(ILoconet2Row row)
    {
        var request = new LocoAdr();
        (request.Adr.Value, request.AdrHigh.Value) = row.Address.Loconet;
        var result = _loconetClient.SendAndWaitReply(request, out SlRdData? slotData);

        if (result != LoconetClient.LoconetSendResult.Success)
            return false;

        if (slotData!.StatBusyActive.AsEnum == ESlotStatusBusyActive.IN_USE)
        {
            _logger.LogTrace("Address {address} found in slot {slot} is IN_USE", row.Address, slotData.Slot);
        }
        else
        {
            _logger.LogTrace("Address {address} found in slot {slot} is {state}, doing NULL_MOVE", row.Address, slotData.Slot, slotData.StatBusyActive);
            var nullMove = new MoveSlots();
            nullMove.Src.Value = slotData.Slot.Value;
            nullMove.Dest.Value = slotData.Slot.Value;
            result = _loconetClient.SendAndWaitReply(nullMove, out slotData);

            if (result != LoconetClient.LoconetSendResult.Success)
                return false;
        }

        var speed = new Speed();
        speed.LocoNet = slotData.Spd.Value;
        var dir = slotData.Dirf[EDirf.Dir] ? Direction.Forward : Direction.Reverse;
        row.DeliverCommandStationState(speed, dir, []);
        return true;
    }
}
