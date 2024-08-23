namespace Loconet.Msg.Accessor;

/// <summary>
/// Access to a 7-bit field plus a single bit from another 7-bit field, forming an entire byte together.
/// </summary>
public class ByteAccess7And1Bit
{
    private readonly Field7Bit _fieldLsbits;
    private readonly BitField7BitBase _bitFieldMsbit;
    private readonly byte _bitMaskMsbit;

    public ByteAccess7And1Bit(Field7Bit fieldLsbits, BitField7BitBase bitFieldMsbit, byte bitMaskMsbit)
    {
        _fieldLsbits = fieldLsbits;
        _bitFieldMsbit = bitFieldMsbit;
        _bitMaskMsbit = bitMaskMsbit;
    }

    public static ByteAccess7And1Bit Make<TBitField>(Field7Bit fieldLsbits, BitField7Bit<TBitField> bitFieldMsbit, TBitField bitMaskMsbit)
        where TBitField : Enum
    {
        return new ByteAccess7And1Bit(fieldLsbits, bitFieldMsbit, (byte)(object)bitMaskMsbit);
    }
        

    public byte Value
    {
        get
        {
            byte result = _fieldLsbits.Value;
            if (_bitFieldMsbit[_bitMaskMsbit])
                result |= 0x80;
            return result;
        }

        set
        {
            _bitFieldMsbit[_bitMaskMsbit] = (value & 0x80) != 0;
            _fieldLsbits.Value = (byte)(value & 0x7F);
        }
    }
}
