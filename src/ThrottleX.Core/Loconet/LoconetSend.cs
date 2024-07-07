using Loconet;
using Loconet.Msg;

namespace ThrottleX.Core.Loconet;

public class LoconetSend : IDisposable
{
    private readonly LoconetClient _loconetClient;
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly CancellationToken _cancellation;
    private readonly AutoResetEvent _wake = new (false);
    private readonly ILogger _logger;
    private readonly Thread _thread;

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
                WaitForOperational();
                Initialize(); 
                NormalOperation();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
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
        var rqSlData = new RqSlData(0);

        var success = _loconetClient.SendAndWaitReply(rqSlData, out SlRdData? reply);

        if (success == LoconetClient.LoconetSendResult.Success)
        {
            _logger.LogInformation($"Reply is {reply}");
        }
        else
        {
            _logger.LogInformation($"Failed to query system slot: {success}");
        }
    }

    public void NormalOperation()
    {
        _cancellation.WaitHandle.WaitOne();// nothing to do yet
    }
}
