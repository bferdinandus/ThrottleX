using Loconet.Msg.Accessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class RqSlData : LoconetMessage
{
    public const byte OPC_RQ_SL_DATA = 0xBB;

    public readonly Field7Bit Slot;

    public RqSlData(byte slot)
    : base(new byte[] { OPC_RQ_SL_DATA, 0, slot, 0 })
    {
        Slot = new(this, 2);
    }
}
