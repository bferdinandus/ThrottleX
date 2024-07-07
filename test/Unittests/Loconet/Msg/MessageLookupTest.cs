using Loconet.Msg;
using Loconet.Msg.Accessor;
using System.Reflection;
using Xunit.Abstractions;
using static System.Console;

namespace Unittests;

public class MessageLookupTest(ITestOutputHelper _output)
{
    public static IEnumerable<object[]> EnumerateTypes()
    {
        var uut = new MessageLookup();
        return uut.EnumerateTypes.Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(EnumerateTypes))]
    public void TestAllFormats(Type messageFormat)
    {
        _output.WriteLine(messageFormat.ToString());

        var fields = messageFormat.GetFields();

        foreach (var field in fields)
        {
            if (!field.FieldType.IsGenericType)
                continue;
            if (field.FieldType.GetGenericTypeDefinition() == typeof(BitField7Bit<>))
            {
                var enumType = field.FieldType.GetGenericArguments()[0];
                _output.WriteLine($"{field.Name}: {enumType.Name}");
                foreach (var literal in Enum.GetValues(enumType))
                {
                    _output.WriteLine(literal.ToString());
                    ByteMaskHelper.ThrowOnInvalidMask((byte)(object)literal);
                }
            }
        }
    }
}
