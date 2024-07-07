namespace Loconet.Msg.Accessor;

public static class ByteMaskHelper
{
    public static byte ByteMask<TEnum>(this TEnum bitMask) where TEnum : Enum
    {
        return (byte) (object) bitMask;
    }

    private static readonly int[] _valid =
    {
        0b00000001,
        0b00000010,
        0b00000100,
        0b00001000,
        0b00010000,
        0b00100000,
        0b01000000,
    };

    public static bool IsValidBitMask(int mask)
    {
        return _valid.Contains(mask);
    }

    public static void ThrowOnInvalidMask(int mask)
    {
        if (!IsValidBitMask(mask))
            throw new ArgumentException($"Must be single bit, but is 0x{mask:X02}", nameof(mask));
    }
}
