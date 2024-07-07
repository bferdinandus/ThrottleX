using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg.Accessor;

public class BitField7BitBase : Field7Bit
{
    public BitField7BitBase(FormatBase msg, int index) 
    : base(msg, index)
    {
    }

    /// <summary>
    /// Read and write a bit identified by the bit mask
    /// </summary>
    /// <param name="bitMask"></param>
    /// <returns>true if the bit (or any of the group of bits) is set</returns>
    public bool this[byte mask]
    {
        get
        {
            return (Value & mask) != 0;
        }

        set
        {
            if (value)
                Value |= mask;
            else
                Value &= (byte)~mask;
        }
    }
}
