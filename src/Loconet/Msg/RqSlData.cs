using Loconet.Msg.Accessor;

namespace Loconet.Msg;

/// <summary>
/// Throttle to command station: give me slot data for this slot ID
/// </summary>
public class RqSlData : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xBB;
    public static byte Length => 4;
    public static bool IsVariableLength => false;

    public readonly Field7Bit Slot;

    public RqSlData(byte slot)
    {
        Slot = new(this, 2);
    }
}
