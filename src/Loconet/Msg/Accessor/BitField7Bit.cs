using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg.Accessor;

/// <summary>
/// A one byte field in a LocoNet message with an indexer for access to individual bits
/// </summary>
/// <typeparam name="TBitMasks">All bit masks that identify individually accessible bits. 
/// This _could_ be multiple bits, but that means you could only set them all or nothing and reading means reading the or'ed result</typeparam>
public class BitField7Bit<TBitMasks> : BitField7BitBase where TBitMasks : Enum
{
    public BitField7Bit(int index) 
    : base(index)
    {
    }

    /// <summary>
    /// Read and write a bit identified by the "index" mask
    /// </summary>
    /// <param name="bitMask"></param>
    /// <returns>true if the bit (or any of the group of bits) is set</returns>
    public bool this[TBitMasks bitMask]
    {
        get
        {
            var mask = (byte)(object)bitMask;
            return base[mask];
        }

        set
        {
            var mask = (byte)(object)bitMask;
            base[mask] = value;
        }
    }
}
