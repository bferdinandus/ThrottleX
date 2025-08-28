using WiThrottle.Enums;

namespace WiThrottle.Models;

public class WiThrottleMessage
{
    public WtCommand Type { get; init; }
    public string Command { get; init; } = string.Empty;
}
