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
        return uut.EnumerateAll.Select(olt => new object[] { olt.Item1, olt.Item2, olt.Item3, olt.Item4 });
    }

    [Theory]
    [MemberData(nameof(EnumerateAll))]
    public void TestAllFormats(byte opcode, byte length, bool isVariableLength, Type messageFormat)
    {
        _output.WriteLine(messageFormat.ToString());

        CheckOpcode(opcode, length, isVariableLength);

        CheckBytesCoverage(length, isVariableLength, messageFormat);

        var fields = messageFormat.GetFields();

        foreach (var field in fields)
        {
            CheckFields(field);
        }
    }

    private void CheckBytesCoverage(byte length, bool isVariableLength, Type messageFormat)
    {
        var uut = (FormatBase) Activator.CreateInstance(messageFormat)!;

        var bytes = new FieldInfo?[length];

        foreach (var fi in uut.EnumerateFieldInfos)
        {
            var field = (Field7Bit)fi.GetValue(uut)!;
            Assert.NotNull(field);
            if (bytes[field.Index] != null)
                Assert.Fail($"Two fields access byte at index {field.Index}: {bytes[field.Index].Name}, {fi.Name}.");
            
            bytes[field.Index] = fi;
        }
        var first = isVariableLength ? 2 : 1;
        for (var i = first; i < bytes.Length-1; i++)
        {
            Assert.False(bytes[i] == null, $"Byte at index {i} has no field.");
            bytes[i] = null; // checking all for null thereafter
        }
        Assert.All(bytes, fi => Assert.Null(fi)); // opcode, length, checkbyte and all non null iterated before
    }

    private void CheckOpcode(byte opcode, byte length, bool isVariableLength)
    {
        switch (opcode & 0x60)
        {
            case 0x40: Assert.Equal(6, length); Assert.False(isVariableLength); break;
            case 0x20: Assert.Equal(4, length); Assert.False(isVariableLength); break;
            case 0x00: Assert.Equal(2, length); Assert.False(isVariableLength); break;
            case 0x60: Assert.InRange(length, 3, 255); Assert.True(isVariableLength); break;
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
