using Shared.Models;

namespace WiThrottle;

public static class WiThrottleHelper
{
    public static IAddress ParseWtAddress(this string address)
    {
        var number = int.Parse(address[1..]);

        return address[0] switch
        {
            'L' => new LongAddress(number),
            'S' => new ShortAddress(number),
            _ => throw new ArgumentException("Must start with L or S", nameof(address))
        };
    }

    public static string EncodeWtAddress(this IAddress address) => address.IsLong ? $"L{address.Address}" : $"S{address.Address}";
}
