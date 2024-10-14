using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class WrSlData : SlotDataBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xEF;
    public static byte Length => 14;
}
