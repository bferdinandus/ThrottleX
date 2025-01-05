namespace Shared.LocoTable;

public record FunctionState(int Number, bool IsOn)
{
    public override string ToString()
    {
        return $"F{Number} is {(IsOn ? "ON" : "off")}";
    }
}
