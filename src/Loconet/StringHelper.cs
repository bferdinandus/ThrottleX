using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet;

public static class StringHelper
{
    public static int HexChar2Value(this char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => throw new InvalidOperationException($"invalid hex character: '{c}'")
        };
    }

    /// <summary>
    /// Hex dump with a space between the byes as used for LoconetOverTcp
    /// </summary>
    public static string ToHex(this byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace('-', ' ');
    }

    public static string DisplayBits<T>(int value) where T : struct, Enum
    {
        return DisplayBits<T>(value, BoolByName);
    }

    public static string DisplayBits<T>(int value, Func<bool, string> bool2String) where T : struct, Enum
    {
        return string.Join(", ", EnumerateBits<T>(value, bool2String));
    }

    public static IEnumerable<string> EnumerateBits<T>(int value, Func<bool, string> bool2String) where T : struct, Enum
    {
        int allBitMasks = 0;

        foreach (int bitMask in Enum.GetValuesAsUnderlyingType<T>())
        {
            allBitMasks |= bitMask;

            var name = typeof(T).GetEnumName(bitMask);
            var val = bool2String((value & bitMask) != 0);
            yield return $"{name}({val})";
        };

        int invisibleBits = ~allBitMasks & value;
        if (invisibleBits != 0)
            yield return $"EXTRA-BITS(0x{invisibleBits:X})";
    }

    private static string BoolByName(bool b)
    {
        return b.ToString();
    }
}
