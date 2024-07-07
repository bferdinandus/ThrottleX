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
}
