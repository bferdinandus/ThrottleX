
using Loconet.Msg.Accessor;
using static Loconet.Msg.Accessor.BitGroupAccessUInt;

namespace Loconet.Msg;

/// <summary>
/// OPC_IMM_PACKET
/// </summary>
public class ImmPacket : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xED;

    public static byte Length => 11;

    public static bool IsVariableLength => true;

    public enum EReps : byte
    {
        Count0 = 0x01,
        Count1 = 0x02,
        Count2 = 0x04,
        Reserved = 0x08,
        NumBytes0 = 0x10,
        NumBytes1 = 0x20,
        NumBytes2 = 0x40,
    }

    public enum EDataHighBits : byte
    {
        Im1_7=0x01,
        Im2_7=0x02,
        Im3_7=0x04,
        Im4_7=0x08,
        Im5_7=0x10
    }

    public readonly BitField7Bit<EReps> Reps;
    public readonly BitField7Bit<EDataHighBits> Dhi;
    public readonly Field7Bit Im1;
    public readonly Field7Bit Im2;
    public readonly Field7Bit Im3;
    public readonly Field7Bit Im4;
    public readonly Field7Bit Im5;

    public readonly BitGroupAccessUInt ByteNum;
    public readonly BitGroupAccessUInt RepeatCount;
    public readonly ByteAccess7And1Bit Im1Byte;
    public readonly ByteAccess7And1Bit Im2Byte;
    public readonly ByteAccess7And1Bit Im3Byte;
    public readonly ByteAccess7And1Bit Im4Byte;
    public readonly ByteAccess7And1Bit Im5Byte;

    public ImmPacket()
    {
        _ = new ConstantField(this, 2, 0x7F);

        Reps = new(this, 3);
        Dhi  = new(this, 4);
        Im1  = new(this, 5);
        Im2  = new(this, 6);
        Im3  = new(this, 7);
        Im4  = new(this, 8);
        Im5  = new(this, 9);

        ByteNum     = new(Bit(Reps, EReps.NumBytes0), Bit(Reps, EReps.NumBytes1), Bit(Reps, EReps.NumBytes2));
        RepeatCount = new(Bit(Reps, EReps.Count0),    Bit(Reps, EReps.Count1),    Bit(Reps, EReps.Count2));
        Im1Byte = ByteAccess7And1Bit.Make(Im1, Dhi, EDataHighBits.Im1_7);
        Im2Byte = ByteAccess7And1Bit.Make(Im2, Dhi, EDataHighBits.Im2_7);
        Im3Byte = ByteAccess7And1Bit.Make(Im3, Dhi, EDataHighBits.Im3_7);
        Im4Byte = ByteAccess7And1Bit.Make(Im4, Dhi, EDataHighBits.Im4_7);
        Im5Byte = ByteAccess7And1Bit.Make(Im5, Dhi, EDataHighBits.Im5_7);
    }
}
