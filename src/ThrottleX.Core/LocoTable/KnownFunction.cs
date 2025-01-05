using Shared.LocoTable;

namespace ThrottleX.Core.LocoTable;

/// <summary>
/// Our knowledge about a certain function of a certain loco address.
/// </summary>
public class KnownFunction
{
    /// <summary>
    /// F0=0, F1=1, ...
    /// </summary>
    public int Number { get; }

    public string Name => $"F{Number}";

    /// <summary>
    /// true=pressed;
    /// false=released (init value)
    /// </summary>
    public bool Pressed = false;

    /// <summary>
    /// Configuration. Default is false="locked". Gets overwritten by wiFRED
    /// on connection establishment with the configuration set in the web interface 
    /// of wiFRED
    /// </summary>
    public bool IsMomentary = false;

    /// <summary>
    /// Current state of the function;
    /// true=on;
    /// false=off;
    /// If this function is part of the slot information, it gets initialized from the slot.
    /// </summary>
    public bool On = false;

    public KnownFunction(int number)
    {
        Number = number;
    }

    public FunctionState State => new FunctionState(Number, On);

    public override string ToString()
    {
        return $"{Name}({(On?"ON":"off")})";
    }
}
