using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    internal class ShortAddress : IAddress
    {
        public const int Largest = 127;

        public ShortAddress(int shortAddress)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(shortAddress, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(shortAddress, Largest);
            Address = shortAddress;
        }
        public bool IsLong => false;

        public int Address { get; }

        public (byte low, byte high) Loconet => (low: (byte)Address, high: 0);

        public override bool Equals(object? obj)
        {
            var other = obj as ShortAddress;
            if (other == null) return false;
            return other.Address == Address;
        }

        public override int GetHashCode()
        {
            return Address;
        }
    }
}
