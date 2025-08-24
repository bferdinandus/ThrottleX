using Hydro;
using WiThrottle;

namespace ThrottleX.Core.Pages.Components;

public class WiFredClients : HydroComponent
{
    public string TestString { get; set; } = "Hello World";
    public WiFredClient[] Clients { get; init; }

    public WiFredClients(WifredDeviceStore deviceStore)
    {
        Clients = deviceStore.GetAllClients().Select(c => new WiFredClient
        {
            Name = c.Name,
            Uid = c.Id,
            IpAddress = c.GetIpAddress(),
            //Locos = c.GetLocoAdresses(),
            Status = c.IsConnected ? WiFredStatus.Online : WiFredStatus.Offline,
            TimeSinceLastMessage = c.ConnectedAt
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
