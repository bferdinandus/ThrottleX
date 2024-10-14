using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class SlotStat1 : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xB5;

    public static byte Length => 4;

    public static bool IsVariableLength => false;

    public readonly Field7Bit Slot;

    public readonly BitField7Bit<ESlotStatus1> Stat1;

    public SlotStat1()
    {
        Slot = new(1);
        Stat1 = new(2);
    }
}
