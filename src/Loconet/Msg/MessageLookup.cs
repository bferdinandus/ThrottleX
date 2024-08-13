using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class MessageLookup : MessageEnumerator
{
    private readonly Dictionary<(byte Opcode, byte Length), Type> _table = [];

    public IEnumerable<Type> EnumerateTypes => _table.Values;
    public IEnumerable<(byte, byte, Type)> EnumerateAll => _table.Select(pair => (pair.Key.Opcode, pair.Key.Length, pair.Value));

    protected override void AddMessage(byte opcode, byte length, Type type)
    {
        _table.Add((opcode, length), type);
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
}
