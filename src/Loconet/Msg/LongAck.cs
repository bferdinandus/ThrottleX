using Loconet.Msg.Accessor;

namespace Loconet.Msg;

internal class LongAck : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xB4;

    public static byte Length => 4;

    public readonly Field7Bit LOpc;
    public readonly Field7Bit Ack1;

    public LongAck()
    {
        LOpc = new(1);
        Ack1 = new(2);
    }
}
