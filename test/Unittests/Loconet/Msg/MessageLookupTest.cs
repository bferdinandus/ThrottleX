using Loconet.Msg;
using Loconet.Msg.Accessor;
using System.Reflection;
using Xunit.Abstractions;
using static System.Console;

namespace Unittests;

public class MessageLookupTest(ITestOutputHelper _output)
{
    public static IEnumerable<object[]> EnumerateAll()
    {
        var uut = new MessageLookup();
        return uut.EnumerateAll.Select(olt => new object[] { olt.Item1, olt.Item2, olt.Item3 });
    }

    [Theory]
    [MemberData(nameof(EnumerateAll))]
    public void TestAllFormats(byte opcode, byte length, Type messageFormat)
    {
        _output.WriteLine(messageFormat.ToString());

        CheckOpcode(opcode, length);

        var fields = messageFormat.GetFields();

        foreach (var field in fields)
        {
            CheckFields(field);
        }
    }

    private void CheckOpcode(byte opcode, byte length)
    {
        switch (opcode & 0x60)
        {
            case 0x40: Assert.Equal(6, length); break;
            case 0x20: Assert.Equal(4, length); break;
            case 0x00: Assert.Equal(2, length); break;
            case 0x60: Assert.InRange(length, 3, 255); break;
        }
    }

    private void CheckFields(FieldInfo field)
    {
        if (!field.FieldType.IsGenericType)
            return;

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
