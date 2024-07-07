using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class MessageLookup
{
    private readonly Dictionary<(byte Opcode, byte Length), Type> _table = [];

    public IEnumerable<Type> EnumerateTypes => _table.Values;

    private void AddOne<T>() where T : FormatBase, ILoconetMessageFormat
    {
        _table.Add((T.Opcode, T.Length), typeof(T));
    }

    public FormatBase? ParseMessage(byte[] msg)
    {
        var opcodeAndLength = (msg[0], (byte)msg.Length);

        if (!_table.TryGetValue(opcodeAndLength, out var formatType))
            return null;

        var formatInstance = formatType?.GetConstructor([])?.Invoke([]);

        if (formatInstance == null) // Message format is unknown (or worse).
            return null;

        var instance = (FormatBase) formatInstance;

        instance.Decode(msg);

        return instance;
    }

    public MessageLookup()
    {
        AddOne<RqSlData>();
        AddOne<WrSlData>();
        AddOne<SlRdData>();
    }
}
