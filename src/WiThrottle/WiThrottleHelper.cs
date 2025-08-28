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

    public static bool TryParseWtAddress(this string input, out IAddress? output)
    {
        output = null;
        if (input.Equals("*"))
        {
            return false;
        };
        
        output = input.ParseWtAddress();
        
        return true;
    }
    
    public static string EncodeWtAddress(this IAddress address) => address.IsLong ? $"L{address.Address}" : $"S{address.Address}";
    
    public static bool ParseBinary(this string input) =>
        input switch
        {
            "0" => false,
            "1" => true,
            _ => throw new ArgumentException($"{input} must be '0' or '1'", nameof(input))
        };
    
    public static (int number, bool state) ParseFunction(this string input)
    {
        var number = int.Parse(input[1..]);
        var state = ParseBinary(input[..1]);
        return (number, state);
    }
}
