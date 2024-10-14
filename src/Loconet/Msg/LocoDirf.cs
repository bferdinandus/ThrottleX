using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoDirf : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA1;

    public static byte Length => 4;

    public readonly Field7Bit Slot;

    public readonly BitField7Bit<EDirf> Dirf;

    public LocoDirf()
    {
        Slot = new(1);
        Dirf = new(2);
    }
}
