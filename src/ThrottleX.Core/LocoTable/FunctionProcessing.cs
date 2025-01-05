using Shared.LocoTable;
using System.Collections.Concurrent;

namespace ThrottleX.Core.LocoTable;

public class FunctionProcessing
{
    /// <summary>
    /// Key is the function number (F0=0, F1=1, ...)
    /// This dictionary learns about the actually used functions during usage.
    /// A FunctionProcessing gets added when the function number is accessed the
    /// first time. This might be wiFRED telling us if it wants to use it as
    /// momentary or locked function or the command station telling us the
    /// initial state.
    /// </summary>
    private readonly Dictionary<int, KnownFunction> _function = new();

    /// <summary>
    /// Output queue. All detected function operations are enqueued here.
    /// </summary>
    private readonly ConcurrentQueue<FunctionState> _functionQueue = new();

    /// <summary>
    /// wiThrottle 'F'
    /// </summary>
    /// <param name="newButtonState">parameter of the wiThrottle function command is the 
    /// new button state - this method performs the change in function state according 
    /// to configured IsMomentary</param>
    public void SetFunctionKey(int number, bool newButtonState)
    {
        PerformFunctionProcessing(number, f =>
        {
            if (f.IsMomentary)
            {
                SetNewState(f, newButtonState);
            }
            else
            {
                if (newButtonState)
                {
                    SetNewState(f, !f.On);
                }
            }
            f.Pressed = newButtonState;
        });
    }

    public void SetMomentaryFunction(int number, bool newMomentaryConfig)
    {
        PerformFunctionProcessing(number, f => f.IsMomentary = newMomentaryConfig);
    }

    public string InitializeFromCommandStation((int Number, bool IsOn)[] fromSlot)
    {
        var logList = new List<string>();
        foreach (var tuple in fromSlot)
        {
            PerformFunctionProcessing(tuple.Number, f =>
            {
                f.On = tuple.IsOn;
                logList.Add(f.ToString());
            });
        }
        return string.Join(", ", logList);
    }

    /// <summary>
    /// wiThrottle 'f'
    /// </summary>
    /// <param name="newFunctionState">parameter of the wiThrottle force function command 
    /// is the forced new function state</param>
    public void ForceFunction(int number, bool newFunctionState)
    {
        PerformFunctionProcessing(number, f =>
        {
            SetNewState(f, newFunctionState);
        });
    }

    private void SetNewState(KnownFunction f, bool newState)
    {
        f.On = newState;
        _functionQueue.Enqueue(f.State);
    }

    /// <summary>
    /// Performs the given action on a function identified by its number.
    /// This action is done with a lock on _function.
    /// If the FunctionProcessing object of this function number is not yet known, a new instance of 
    /// is created, stored and used.
    /// </summary>
    /// <param name="number">F0=0, F1=1, ...</param>
    /// <param name="action">this gets called inside a lock onto _function</param>
    private void PerformFunctionProcessing(int number, Action<KnownFunction> action)
    {
        lock (_function)
        {
            var alreadyKnown = _function.TryGetValue(number, out var processing);

            if (!alreadyKnown)
            {
                processing = new KnownFunction(number);
                _function.Add(number, processing);
            }

            action(processing!);
        }
    }

    public bool Dequeue(out FunctionState? functionState)
    {
        return _functionQueue.TryDequeue(out functionState);
    }
}
