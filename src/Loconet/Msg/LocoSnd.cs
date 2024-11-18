using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoSnd : LocoBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA2;

    public static byte Length => 4;

    public readonly BitField7Bit<ESlotSound> Snd = new(2);
}
