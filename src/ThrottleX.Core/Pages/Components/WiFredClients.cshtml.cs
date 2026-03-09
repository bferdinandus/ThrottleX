using Hydro;
using WiThrottle;

namespace ThrottleX.Core.Pages.Components;

public class WiFredClients : HydroComponent
{
    private readonly WifredClientStore _clientStore;
    public DateTime LastRefresh { get; set; } = DateTime.UtcNow;
    public WiFredClient[] Clients { get; private set; } = [];

    public WiFredClients(WifredClientStore clientStore)
    {
        _clientStore = clientStore;
        RefreshClients();
    }
    
    private void RefreshClients()
    {
        Clients = _clientStore.GetAllClients().Select(c => new WiFredClient
        {
            Name = c.Name,
            Uid = c.Id,
            IpAddress = c.GetIpAddress(),
            Locos = c.GetLocoAdresses(),
            Status = c.IsConnected ? WiFredStatus.Online : WiFredStatus.Offline,
            LastMessage = c.LastMessage,
            ConnectedAt = c.ConnectedAt
        }).ToArray();
    }

    public void Refresh()
    {
        LastRefresh = DateTime.UtcNow;
    }
}

public class WiFredClient
{
    public string Name { get; init; } = string.Empty;
    public string Uid { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Locos { get; init; } = string.Empty;
    public WiFredStatus Status { get; init; }
    public DateTime LastMessage { get; init; }
    public DateTime? ConnectedAt { get; init; }
}

public enum WiFredStatus
{
    Online,
    Offline
}
