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

    public readonly ConstantField Spare = new(1, 0);
    public readonly Field7Bit Slot = new(2);

    public RqSlData() { }

    public RqSlData(byte slot)
    {
        Slot.Value = slot;
    }
}
