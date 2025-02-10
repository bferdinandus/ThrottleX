using Loconet.Msg.Accessor;
using Microsoft.VisualBasic;
using System.ComponentModel;

namespace Loconet.Msg;

/// <summary>
/// Message defined by Uhlenbrock(?) for upper functions and binary states
/// 
/// Binary states reverse engineered from IB II and ProfiBoss:
///
/// Message: <0xd4> <state> <slot> <adr_lsb> <adr_msb> <cksum>
///
///		state_3: 	states > 16372
///		state_4:	1 = on
/// </summary>
public class UhlenbrockD4 : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xD4;

    public static byte Length => 6;

    public enum EState : byte
    {
        BinaryStatesMsb = 0x08, // set for states > 16372
        On = 0x10,              // set=function on, reset=function off
        Functions = 0x20,       // set for functions, reset for binary states
    }

    protected BitField7Bit<EState> State = new(1);

    /// <summary>
    /// Loco slot
    /// </summary>
    public Field7Bit Slot = new(2);

    /// <summary>
    /// Bits 7..13 of binary state address _or_ function range usage
    /// </summary>
    protected Field7Bit AdrMsb = new(3);

    /// <summary>
    /// Bits 0..6 of binary stae address _or_ function bitmap
    /// </summary>
    protected Field7Bit AdrLsb = new(4);

    public enum EUsage
    {
        Invalid = -1,
        BinaryState = -2,
        F20F28 = 5,
        F9F11 = 7, // F9-F12 only IB COM, IB Basic, Twin-Center (IB-2 uses opc=0xA3)
        F13F19 = 8,
        F21F27 = 9
    }

    /// <summary>
    /// What kind of bit(s) does this message transport?
    /// </summary>
    public EUsage Usage
    {
        get
        {
            if ((State.Value & 0xD7) != 0)
                return EUsage.Invalid;

            if (!State[EState.Functions])
                return EUsage.BinaryState;

            var usage = (EUsage)AdrMsb.Value;
            return usage switch
            {
                EUsage.F20F28 or EUsage.F9F11 or EUsage.F13F19 or EUsage.F21F27 => usage,
                _ => EUsage.Invalid,
            };
        }
        set
        {
            switch (value)
            {
                case EUsage.BinaryState:
                    State[EState.Functions] = false;
                    break;

                case EUsage.F20F28:
                case EUsage.F9F11:
                case EUsage.F13F19:
                case EUsage.F21F27:
                    State[EState.Functions] = true;
                    AdrMsb.Value = (byte)value;
                    break;

                default: throw new InvalidEnumArgumentException(nameof(value), (int)value, value.GetType());
            }
        }

    }

    /// <summary>
    /// If Usage=BinaryStates than this is the address of the binary state
    /// </summary>
    public uint BinaryStateAddress
    {
        get
        {
            if (Usage != EUsage.BinaryState)
                throw new InvalidOperationException($"Can't read binary state address if Usage is {Usage}");

            var number = (uint)AdrMsb.Value;
            number <<= 7;
            number |= AdrLsb.Value;
            if (State[EState.BinaryStatesMsb])
                number |= 0x4000; // most significant bit
            return number;
        }
        set
        {
            if (Usage != EUsage.BinaryState)
                throw new InvalidOperationException($"Can't write binary state address if Usage is {Usage}");

            if (value > 0x4fff)
                throw new ArgumentOutOfRangeException(nameof(value), value, "too big binary state address");

            AdrLsb.Value = (byte)(value & 0x7F);
            AdrMsb.Value = (byte)((value >> 7) & 0x7F);
            var mostSignificantBit = (value & 0x4000) != 0;
            State[EState.BinaryStatesMsb] = mostSignificantBit;
        }
    }

    public string BinaryStateAddressString 
    { 
        get 
        { 
            var adr = BinaryStateAddress;
            return adr==0 ? "Broadcast" : adr.ToString() ; 
        } 
    }

    [Flags]
    public enum EF20F28Data
    {
        F12 = 0x10,
        F20 = 0x20,
        F28 = 0x40,
    }

    /// <summary>
    ///	Message: <0xd4> <0x20> <slot> <0x05> <data> <cksum>
    ///	 data_4:	F12 bei IB-Com, IB-Basic, Twin-Center
    ///  data_5:	F20
    ///  data_6:	F28
    /// </summary>
    public EF20F28Data F20F28Data => ReadDataAsEnum<EF20F28Data>(EUsage.F20F28);

    [Flags]
    public enum EF9F11Data
    {
        F9 = 0x10,
        F10 = 0x20,
        F11 = 0x40,
    }

    /// <summary>
    /// F9-F12 only IB COM, IB Basic, Twin-Center (IB-2 uses opc=0xA3):
    /// Message: <0xd4> <0x20> <slot> <0x07> <data> <cksum>
    ///  data_4: F9
    ///  data_5 F10
    ///  data_6 F11
    /// </summary>
    public EF9F11Data F9F11Data => ReadDataAsEnum<EF9F11Data>(EUsage.F9F11);

    [Flags]
    public enum EF13F19Data
    {
        F13 = 0x01,
        F14 = 0x02,
        F15 = 0x04,
        F16 = 0x08,
        F17 = 0x10,
        F18 = 0x20,
        F19 = 0x40,
    }

    /// <summary>
    /// Message: <0xd4> <0x20> <slot> <0x08> <data> <cksum>
    ///  data_0:	F13
    ///  data_1:	F14
    ///  data_2:	F15
    ///  data_3:	F16
    ///  data_4:	F17
    ///  data_5:	F18
    ///  data_6:	F19
    /// </summary>
    public EF13F19Data F13F19Data => ReadDataAsEnum<EF13F19Data>(EUsage.F13F19);

    [Flags]
    public enum EF21F27Data
    {
        F21 = 0x01,
        F22 = 0x02,
        F23 = 0x04,
        F24 = 0x08,
        F25 = 0x10,
        F26 = 0x20,
        F27 = 0x40,
    }

    /// <summary>
    /// Message: <0xd4> <0x20> <slot> <0x09> <data> <cksum>
    ///  data_0:	F21
    ///  data_1:	F22
    ///  data_2:	F23
    ///  data_3:	F24
    ///  data_4:	F25
    ///  data_5:	F26
    ///  data_6:	F27
    /// </summary>
    public EF21F27Data F21F27Data => ReadDataAsEnum<EF21F27Data>(EUsage.F21F27);

    private T ReadDataAsEnum<T>(EUsage expectedUsage) where T : Enum
    {
        if (Usage != expectedUsage)
            throw new InvalidOperationException($"Can't read {typeof(T).Name} if Usage is {Usage}");

        return (T)Enum.ToObject(typeof(T), AdrLsb.Value);
    }

    public override string ToString()
    {
        switch (Usage)
        {
            case EUsage.BinaryState: return $"Binary state {BinaryStateAddressString} is {ONoff(State[EState.On])}";
            case EUsage.F20F28: return Bitmap<EF20F28Data>();
            case EUsage.F9F11:  return Bitmap<EF9F11Data>();
            case EUsage.F13F19: return Bitmap<EF13F19Data>();
            case EUsage.F21F27: return Bitmap<EF21F27Data>();
            default: return $"{Usage} opcode D4(state={State.Value}, slot={Slot.Value}, adr_high={AdrMsb.Value}, adr_low={AdrLsb.Value})";
        }
    }

    private string Bitmap<T>() where T : struct,Enum
    {
        string functions = StringHelper.DisplayBits<T>(AdrLsb.Value, ONoff);
        return $"Slot={Slot.Value}: {string.Join(", ", functions)}";
    }

    private string ONoff(bool isOn)
    {
        return isOn ? "ON" : "off";
    }
}
