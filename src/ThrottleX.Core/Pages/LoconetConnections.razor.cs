namespace ThrottleX.Core.Pages;

public partial class LoconetConnections
{
    private int TotalCount => LoconetService.Clients.Count();
    private int OperationalCount => LoconetService.Clients.Count(c => c.client.IsOperational);

    protected override void OnInitialized()
    {
        LoconetService.OnConnectionsChanged += HandleConnectionsChanged;
    }

    private void HandleConnectionsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LoconetService.OnConnectionsChanged -= HandleConnectionsChanged;
    }
}
