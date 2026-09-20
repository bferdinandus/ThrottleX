using WiThrottle;

namespace ThrottleX.Core.Pages;

public partial class WiFredClients
{
    private string _searchTerm = string.Empty;
    private IEnumerable<WifredClient> Clients => ClientStore.GetAllClients();

    private IEnumerable<WifredClient> FilteredClients
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchTerm))
                return Clients;

            var term = _searchTerm.Trim();
            return Clients.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.GetIpAddress().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.GetLocoAdresses().Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected override void OnInitialized()
    {
        ClientStore.OnStoreChanged += HandleStoreChanged;
    }

    private void HandleStoreChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void ForgetClient(string id)
    {
        ClientStore.ForgetClient(id);
    }

    public void Dispose()
    {
        ClientStore.OnStoreChanged -= HandleStoreChanged;
    }
}
