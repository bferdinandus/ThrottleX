using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class MessageLookup : MessageEnumerator
{
    private readonly Dictionary<(byte Opcode, byte Length), (bool IsVariableLength, Type type)> _table = [];

    public IEnumerable<Type> EnumerateTypes => _table.Values.Select(p => p.type);
    public IEnumerable<(byte, byte, bool, Type)> EnumerateAll => _table.Select(pair => (pair.Key.Opcode, pair.Key.Length, pair.Value.IsVariableLength, pair.Value.type));

    protected override void AddMessage(byte opcode, byte length, bool isVariableLength, Type type)
    {
        _table.Add((opcode, length), (isVariableLength, type));
    }

    public FormatBase? ParseMessage(byte[] msg)
    {
        var opcodeAndLength = (msg[0], (byte)msg.Length);

        if (!_table.TryGetValue(opcodeAndLength, out var format))
            return null;

        var formatInstance = format.type?.GetConstructor([])?.Invoke([]);

        if (formatInstance == null) // Message format is unknown (or worse).
            return null;

        var instance = (FormatBase) formatInstance;

        instance.Decode(msg);

        return instance;
    }
}
