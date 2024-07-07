using static Loconet.Msg.Accessor.ByteMaskHelper;

namespace Loconet.Msg.Accessor;

public class BitGroupAccessEnum<TApiEnum> : BitGroupAccessUInt where TApiEnum : Enum
{
    /// <summary>
    /// New instance with given sequence of bits
    /// </summary>
    /// <param name="lsbitToMsbit">First element in parameter array is for the least significant bit of Value</param>
    public BitGroupAccessEnum((BitField7BitBase, byte)[] lsbitToMsbit) 
    : base(lsbitToMsbit)
    {
    }

    public static BitGroupAccessEnum<TApiEnum> Make<TBitMask>(BitField7Bit<TBitMask> bitField, params TBitMask[] fromLsbitToMsBit) where TBitMask : Enum
    {
        var tupels = fromLsbitToMsBit.Select(bitMask => Bit(bitField, bitMask));
        return new(tupels.ToArray());
    }

    public TApiEnum AsEnum
    {
        get => (TApiEnum)(object)base.Value;
        set => base.Value = (uint)(object)value;
    }
}
