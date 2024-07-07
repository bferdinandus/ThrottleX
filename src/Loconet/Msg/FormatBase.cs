using Loconet.Msg.Accessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

/// <summary>
/// Base class for those LocoNet messages that we know the exact format of, because we want to receive or send it.
/// Derived classes shall also implement ILoconetMessageFormat and have Field7Bit members that store the content of the message.
/// </summary>
public abstract class FormatBase : ReceivableLoconetMessage
{
    private readonly List<Field7Bit> _fields = new();

    public void Decode(byte[] data)
    {
        foreach (var field in _fields)
        {
            field.Value = data[field.Index];
        }
    }

    public byte[] Encode<T>() where T : ILoconetMessageFormat
    {
        var msg = new byte[T.Length];

        msg[0] = T.Opcode;
    
        if (T.IsVariableLength)
            msg[1] = T.Length;

        foreach (var field in _fields)
        {
            msg[field.Index] = field.Value;
        }

        return msg;
    }

    public void AddField(Field7Bit field7Bit)
    {
        _fields.Add(field7Bit);
    }
}
