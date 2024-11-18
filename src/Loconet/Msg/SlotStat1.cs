using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class SlotStat1 : LocoBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xB5;

    public static byte Length => 4;

    public readonly BitField7Bit<ESlotStatus1> Stat1 = new(2);
}
