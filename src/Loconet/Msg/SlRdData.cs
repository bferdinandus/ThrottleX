using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public class SlRdData : SlotDataBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xE7;
    public static byte Length => 14;
    public static bool IsVariableLength => true;
}
