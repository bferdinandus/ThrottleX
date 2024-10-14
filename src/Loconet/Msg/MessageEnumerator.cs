namespace Loconet.Msg;

public abstract class MessageEnumerator  
{
    protected abstract void AddMessage(byte opcode, byte length, bool isVariableLength, Type type);

    private void AddOne<T>() where T : FormatBase, ILoconetMessageFormat
    {
        AddMessage(T.Opcode, T.Length, T.IsVariableLength, typeof(T));
    }

    public MessageEnumerator()
    {
        AddOne<LocoAdr>();
        AddOne<LocoDirf>();
        AddOne<LocoSnd>();
        AddOne<LocoSpd>();
        AddOne<LongAck>();
        AddOne<RqSlData>();
        AddOne<SlRdData>();
        AddOne<WrSlData>();
        AddOne<ImmPacket>();
        AddOne<DigitraxD5>();
    }
}
