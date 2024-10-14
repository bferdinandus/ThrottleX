namespace Loconet.Msg;

public static class MessageHelper
{
    public static bool IsVariableLength(this byte opcode) => (opcode & 0x60) == 0x60;
}
