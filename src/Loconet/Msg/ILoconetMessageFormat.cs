using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

public interface ILoconetMessageFormat//<T> where T : ILoconetMessageFormat<T>
{
    static abstract byte Opcode { get; }
    static abstract byte Length { get; }
    static abstract bool IsVariableLength { get; }
}
