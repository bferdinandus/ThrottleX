namespace WiThrottle;

public class WiThrottleCommand
{
    public CommandType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    
    public override string ToString()
    {
        return $"WiThrottleCommand {{ Type = {Type}, Message = \"{Message}\" }}";
    }
}
