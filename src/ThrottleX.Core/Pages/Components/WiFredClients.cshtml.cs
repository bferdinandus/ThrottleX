using Hydro;
using WiThrottle;

namespace ThrottleX.Core.Pages.Components;

public class WiFredClients : HydroComponent
{
    public string TestString { get; set; } = "Hello World";
    public WiFredClient[] Clients { get; init; }

    public WiFredClients(WiThrottleService wiThrottleService)
    {
        Clients = wiThrottleService.Clients.Select(c => new WiFredClient
        {
            Name = c.Value.Name,
            Uid = c.Value.Uid,
            IpAddress = c.Value.GetIpAddress(),
            Locos = c.Value.GetLocoAdresses(),
            Status = WiFredStatus.Online,
            TimeSinceLastMessage = c.Value.ConnectionTime
        }).ToArray();
    }

    public void Add()
    {
        TestString = $"{TestString}.";
    }
}

public class WiFredClient
{
    public string Name { get; init; } = string.Empty;
    public string Uid { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Locos { get; init; } = string.Empty;
    public WiFredStatus Status { get; init; }
    public DateTime TimeSinceLastMessage { get; init; }
}

public enum WiFredStatus
{
    Online,
    Offline
}
