namespace Loconet.Msg.Accessor;

/// <summary>
/// Access to groups of bits in BitField7Bit instances
/// </summary>
public class BitGroupAccessUInt
{
    private readonly (BitField7BitBase BitField, byte Mask)[] _mappings;

    /// <summary>
    /// New instance with given sequence of bits
    /// </summary>
    /// <param name="lsbitToMsbit">First element in parameter array is for the least significant bit of Value</param>
    public BitGroupAccessUInt( (BitField7BitBase, byte)[] lsbitToMsbit)
    {
        _mappings = lsbitToMsbit;
    }

    public static (BitField7BitBase bitField, byte) Bit<TBitField>(BitField7Bit<TBitField> bitField, TBitField bitMask)
        where TBitField : Enum
    {
        return (bitField, bitMask.ByteMask());
    }

    public uint Value
    {
        get
        {
            uint result = 0;
            uint mask = 1;
            foreach (var mapping in _mappings)
            {
                if (mapping.BitField[mapping.Mask])
                    result |= mask;
                mask <<= 1;
            }
            return result;
        }

        set
        {
            uint input = value;
            foreach (var mapping in _mappings)
            {
                var bitValue = (input & 1) != 0;
                mapping.BitField[mapping.Mask] = bitValue;
                input >>= 1;
            }
        }
    }
}
