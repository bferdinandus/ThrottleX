namespace ThrottleX.Core.Shared;

public partial class NavMenu
{
    private bool _collapseNavMenu = true;
    private string? NavMenuCssClass => _collapseNavMenu ? null : "show";
    private int OnlineClientsCount => ClientStore.GetAllClients().Count(c => c.IsConnected);
    private int OperationalLoconetCount => LoconetService.Clients.Count(c => c.client.IsOperational);

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

    private void ToggleNavMenu()
    {
        _collapseNavMenu = !_collapseNavMenu;
    }

    public void Dispose()
    {
        ClientStore.OnStoreChanged -= HandleStoreChanged;
        LoconetService.OnConnectionsChanged -= HandleConnectionsChanged;
    }
}
