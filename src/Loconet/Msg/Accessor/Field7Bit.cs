using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg.Accessor;

/// <summary>
/// A one byte field in a LocoNet message. 7 LSBs are usable.
/// This class is intended for making instances as members of a FormatBase implementation.
/// </summary>
public class Field7Bit
{
    public readonly int Index;
    private byte _value;

    /// <summary>
    /// New instance for one byte in a LocoNet message.
    /// This constructor is intended to be called from the constructor of the FormatBase implementation.
    /// </summary>
    /// <param name="msg">"this" reference of the FormatBase that gets this instance as member</param>
    /// <param name="index">Byte index of this field inside the message. Counting starts with zero at the opcode</param>
    public Field7Bit(FormatBase msg, int index)
    {
        msg.AddField(this);
        Index = index;
    }

    public virtual byte Value
    {
        get
        {
            return _value;
        }

        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 127, nameof(Value));
            _value = value;
        }
    }

    public override string ToString()
    {
        return $"[{Index}] = 0x{_value:X02}";
    }
}
