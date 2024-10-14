
namespace Loconet.Msg.Accessor;

public class ConstantField : Field7Bit
{
    private readonly byte _constantValue;

    public ConstantField(int index, byte constantValue)
        : base(index)
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
