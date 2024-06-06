using System.Collections.Concurrent;

namespace Shared.Models;

public class WiThrottleLocoTables
{
    public ConcurrentBag<LocoTable> Locos { get; set; } = [];
}
