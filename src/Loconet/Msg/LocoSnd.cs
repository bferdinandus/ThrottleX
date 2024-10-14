using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoSnd : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA2;

    public static byte Length => 4;

    public static bool IsVariableLength => false;

    public readonly Field7Bit Slot;

    public readonly BitField7Bit<ESlotSound> Snd;

    public LocoSnd()
    {
        Slot = new(1);
        Snd = new(2);
    }
}
