namespace Loconet.Msg;

public interface ILoconetMessageFormat//<T> where T : ILoconetMessageFormat<T>
{
    static abstract byte Opcode { get; }
    static abstract byte Length { get; }
}
