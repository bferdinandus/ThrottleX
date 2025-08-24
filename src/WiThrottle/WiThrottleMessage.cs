namespace WiThrottle;

public class WiThrottleMessage
{
    public CommandType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    
    public override string ToString()
    {
        return $"WiThrottleMessage {{ Type = {Type}, Message = \"{Message}\" }}";
    }
}
