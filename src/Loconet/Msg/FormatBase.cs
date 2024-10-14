using Loconet.Msg.Accessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

/// <summary>
/// Base class for those LocoNet messages that we know the exact format of, because we want to receive or send it.
/// Derived classes shall also implement ILoconetMessageFormat and have Field7Bit members that store the content of the message.
/// </summary>
public abstract class FormatBase : ReceivableLoconetMessage
{
    public IEnumerable<FieldInfo> EnumerateFieldInfos
    {
        get => GetType().GetFields().Where(f => f.FieldType.IsAssignableTo(typeof(Field7Bit)));
    }

    public IEnumerable<Field7Bit> EnumerateFields
    {
        get => EnumerateFieldInfos.Select(fi => (Field7Bit) fi!.GetValue(this)!);
    }

    public void Decode(byte[] data)
    {
        foreach (var field in EnumerateFields)
        {
            field.Value = data[field.Index];
        }
    }

    public byte[] Encode<T>() where T : ILoconetMessageFormat
    {
        var msg = new byte[T.Length];

        msg[0] = T.Opcode;
    
        if (T.Opcode.IsVariableLength())
            msg[1] = T.Length;

        foreach (var field in EnumerateFields)
        {
            msg[field.Index] = field.Value;
        }

        byte check = 0xff;
        for (var i=0; i<T.Length-1; i++)
            check ^= msg[i];
        msg[T.Length - 1] = check;

        return msg;
    }
}
