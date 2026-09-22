using WiThrottle;

namespace ThrottleX.Core.Pages;

public partial class Index
{
    private IEnumerable<WifredClient> Clients => ClientStore.GetAllClients().OrderByDescending(c => c.ConnectedAt);
    private int OnlineClientsCount => Clients.Count(c => c.IsConnected);
    private int TotalClientsCount => Clients.Count();
    private int ActiveLocosCount => Clients.Where(c => c.IsConnected).Sum(c => c.GetLocoCount());
    private int OperationalLoconetCount => LoconetService.Clients.Count(c => c.client.IsOperational);
    private int TotalLoconetCount => LoconetService.Clients.Count();

    protected override void OnInitialized()
    {
        ClientStore.OnStoreChanged += HandleStoreChanged;
        LoconetService.OnConnectionsChanged += HandleConnectionsChanged;
    }

    private void HandleStoreChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void HandleConnectionsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ClientStore.OnStoreChanged -= HandleStoreChanged;
        LoconetService.OnConnectionsChanged -= HandleConnectionsChanged;
    }
}
