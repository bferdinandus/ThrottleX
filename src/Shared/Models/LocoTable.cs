using System.Text;

namespace Shared.Models;

public class LocoTable
{
    public char MultiThrottleInstance { get; set; }
    public string LocomotiveKey { get; set; } = string.Empty;

    public int Speed { get; set; } = 0;
    public Direction Direction { get; set; } = Direction.Forward;
    public SpeedStepMode SpeedStepMode { get; set; } = SpeedStepMode.N128Step;
    public FunctionButton F0 { get; set; } = FunctionButton.Off;
    public FunctionButton F1 { get; set; } = FunctionButton.Off;
    public FunctionButton F2 { get; set; } = FunctionButton.Off;
    public FunctionButton F3 { get; set; } = FunctionButton.Off;
    public FunctionButton F4 { get; set; } = FunctionButton.Off;
    public FunctionButton F5 { get; set; } = FunctionButton.Off;
    public FunctionButton F6 { get; set; } = FunctionButton.Off;
    public FunctionButton F7 { get; set; } = FunctionButton.Off;
    public FunctionButton F8 { get; set; } = FunctionButton.Off;
    public FunctionButton F9 { get; set; } = FunctionButton.Off;
    public FunctionButton F10 { get; set; } = FunctionButton.Off;
    public FunctionButton F11 { get; set; } = FunctionButton.Off;
    public FunctionButton F12 { get; set; } = FunctionButton.Off;
    public FunctionButton F13 { get; set; } = FunctionButton.Off;
    public FunctionButton F14 { get; set; } = FunctionButton.Off;
    public FunctionButton F15 { get; set; } = FunctionButton.Off;
    public FunctionButton F16 { get; set; } = FunctionButton.Off;
    public FunctionButton F17 { get; set; } = FunctionButton.Off;
    public FunctionButton F18 { get; set; } = FunctionButton.Off;
    public FunctionButton F19 { get; set; } = FunctionButton.Off;
    public FunctionButton F20 { get; set; } = FunctionButton.Off;
    public FunctionButton F21 { get; set; } = FunctionButton.Off;
    public FunctionButton F22 { get; set; } = FunctionButton.Off;
    public FunctionButton F23 { get; set; } = FunctionButton.Off;
    public FunctionButton F24 { get; set; } = FunctionButton.Off;
    public FunctionButton F25 { get; set; } = FunctionButton.Off;
    public FunctionButton F26 { get; set; } = FunctionButton.Off;
    public FunctionButton F27 { get; set; } = FunctionButton.Off;
    public FunctionButton F28 { get; set; } = FunctionButton.Off;

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F0}0");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F1}1");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F2}2");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F3}3");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F4}4");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F5}5");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F6}6");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F7}7");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F8}8");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F9}9");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F10}10");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F11}11");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F12}12");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F13}13");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F14}14");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F15}15");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F16}16");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F17}17");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F18}18");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F19}19");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F20}20");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F21}21");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F22}22");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F23}23");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F24}24");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F25}25");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F26}26");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F27}27");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}F{(int)F28}28");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}V{Speed}");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}R{(int)Direction}");
        sb.AppendLine($"M{MultiThrottleInstance}A{LocomotiveKey}{Constants.Separator}s{(int)SpeedStepMode}");

        return sb.ToString();
    }
}

public enum Direction
{
    Reverse,
    Forward
}

public enum FunctionButton
{
    Off,
    On
}

public enum SpeedStepMode
{
    N128Step = 1,
    N28Step = 2,
    N27Step = 4,
    N14Step = 8
}
