using Shared.Models;
using WiThrottle.Enums;

namespace WiThrottle.Models;

public class MultiThrottleMessage
{
    public MtCommand Command { get; set; }
    public char ThrottleId { get; set; } // TODO: find out what to do with this identifier
    public IAddress? Address { get; set; }

    //public ThrottleCommand ThrottleCommand { get; set; } 
    public string ThrottleCommandMessage { get; set; } = string.Empty;
}
