using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoDirf : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA2;

    public static byte Length => 4;

    public static bool IsVariableLength => false;

    public readonly Field7Bit Slot;

    public readonly BitField7Bit<EDirf> Dirf;

    public LocoDirf()
    {
        Slot = new(this, 1);
        Dirf = new(this, 2);
    }
}
