
namespace Loconet.Msg.Accessor;

public class ConstantField : Field7Bit
{
    private readonly byte _constantValue;

    public ConstantField(FormatBase msg, int index, byte constantValue)
        : base(msg, index)
    {
        _constantValue = constantValue;
    }

    public override byte Value 
    {
        get => _constantValue;
        set
        {
            if (value != _constantValue) 
                throw new InvalidOperationException($"Writing unexpected 0x{value:X} to constant value expected to be 0x{_constantValue:X}"); 
        }
    }
}
