using Loconet.Msg.Accessor;
using System;
using static Loconet.Msg.ImmPacket;

namespace Loconet.Msg;

/// <summary>
/// Digitrax Opcode D5
/// <0xd5> <atyp> <slot><0x36> <data> <chksum>
/// </summary>
public class DigitraxD5 : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xD5;

    public static byte Length => 6;

    public static bool IsVariableLength => false;

    /// <summary>
    /// atyp_0: Slot 0 bis 119
    /// atyp_1: Slot 120 bis 239
    /// atyp_2: Slot 240 bis 369
    /// atyp_3: dir(bei atyp4-atyp7= 0)
    /// atyp_4: F0-F6
    /// atyp_4 && atyp_3: F7-F13
    /// atyp_5: F14_F20
    /// atyp_5 && atyp_3: F21-F27
    /// </summary>
    public enum EATyp : byte
    {
        Slot0 = 0b0000001,
        Slot1 = 0b0000010,
        Slot2 = 0b0000100,
        Mode0 = 0b0001000,
        Mode1 = 0b0010000,
        Mode2 = 0b0100000,
        Spare = 0b1000000,
    }

    public enum ESlotRange
    {
        Slot0to119,
        Slot120to239,
        Slot240to369
    }

    public enum EMode
    {
        Forward = 0,
        Backward = 1,
        F0toF6 = 2,
        F7toF13 = 3,
        F14toF20 = 4,
        F21toF27 = 5,
        F28on = 6,
        F28off = 7,
    }

    public readonly BitField7Bit<EATyp> ATyp;
    public readonly Field7Bit Slot;
    public readonly ConstantField Spare;
    public readonly Field7Bit Data;

    public readonly BitGroupAccessEnum<ESlotRange> AccessSlotRange;
    public readonly BitGroupAccessEnum<EMode> AccessMode;

    public DigitraxD5()
    {

        ATyp = new(1);
        Slot = new(2);
        Spare = new ConstantField(3, 0x36);
        Data = new(4);

        AccessSlotRange = BitGroupAccessEnum<ESlotRange>.Make(ATyp, EATyp.Slot0, EATyp.Slot1, EATyp.Slot2);
        AccessMode      = BitGroupAccessEnum<EMode>.     Make(ATyp, EATyp.Mode0, EATyp.Mode1, EATyp.Mode2);
    }
}
