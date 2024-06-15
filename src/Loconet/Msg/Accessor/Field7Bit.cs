using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg.Accessor;

public class Field7Bit
{
    private readonly LoconetMessage _myMsg;
    private readonly int _index;

    public Field7Bit(LoconetMessage msg, int index)
    {
        _myMsg = msg;
        _index = index;
    }

    public byte Value
    { 
        get => _myMsg.Bytes[_index];
        set => _myMsg.Bytes[_index] = value;
    }
}
