using Loconet.Msg.Accessor;

namespace Loconet.Msg;

/// <summary>
/// Base class for four-byte messages that manipulate a single slot byte with the slot number
/// being the first parameter.
/// </summary>
public abstract class LocoBase : FormatBase
{
    public readonly Field7Bit Slot = new(1);
}
