using Loconet.Msg.Accessor;

namespace Loconet.Msg;

internal class LocoAdr : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xBF;

    public static byte Length => 4;

    public static bool IsVariableLength => false;

    /// <summary>
    /// Most significant bits of address, locope pretends "0" for this ;-)
    /// </summary>
    public Field7Bit AdrHigh;

    /// <summary>
    /// Least significant bits of address
    /// </summary>
    public Field7Bit Adr;

    public LocoAdr()
    {
        AdrHigh = new(1);
        Adr = new(2);
    }
}
