using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public interface IAddress
    {
        bool IsLong { get; }

        /// <summary>
        /// Human readable address as a single number
        /// </summary>
        int Address { get; }

        /// <summary>
        /// Representation of an address distributed into two seven bit values
        /// </summary>
        (byte low, byte high) Loconet { get; }

        public static IAddress FromLoconet(byte low, byte high)
        {
            if (high == 0)
                return new ShortAddress(low);
            else
                return new LongAddress(low, high);
        }
    }
}
