using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class LongAddress : IAddress
    {
        public const int Largest = 10239;

        public LongAddress(int longAddress)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(longAddress, ShortAddress.Largest);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(longAddress, Largest);
            Address = longAddress;
        }
        public bool IsLong => true;

        public int Address { get; }

        public (byte low, byte high) Loconet => (low: (byte) (Address % 0x80), high: (byte) (Address / 0x80));

        public override bool Equals(object? obj)
        {
            var other = obj as LongAddress;
            if (other == null) return false;
            return other.Address == Address;
        }

        public override int GetHashCode()
        {
            return Address;
        }
    }
}
