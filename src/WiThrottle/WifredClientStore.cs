using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.LocoTable;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace WiThrottle;

public class WifredClientStore
{
    private readonly ILogger<WifredClientStore> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IThrottle2Table _locoTable;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, WifredClient> _clients = new();

    public event Action? OnStoreChanged;

    public void NotifyStoreChanged()
    {
        OnStoreChanged?.Invoke();
    }

    public WifredClientStore(ILogger<WifredClientStore> logger, ILoggerFactory loggerFactory, IThrottle2Table locoTable, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _locoTable = locoTable;
        _httpClientFactory = httpClientFactory;
    }

    public WifredClient GetOrCreate(string uid, string name)
    {
        bool isNew = false;
        var client = _clients.GetOrAdd(uid, _ =>
        {
            isNew = true;
            var httpClient = _httpClientFactory.CreateClient("WiFredClient");
            WifredClient newClient = new WifredClient(uid, name, _loggerFactory.CreateLogger<WifredClient>(), _locoTable, httpClient);
            newClient.OnClientChanged += NotifyStoreChanged;
            _logger.LogInformation("Created new WifredClient with name: `{name}` and uid: {uid}", name, uid);

            return newClient;
        });

        if (isNew)
        {
            NotifyStoreChanged();
        }

        return client;
    }

    public WifredClient? GetClient(string id)
    {
        _clients.TryGetValue(id, out WifredClient? client);

        return client;
    }

    public void ForgetClient(string id)
    {
        if (_clients.TryRemove(id, out WifredClient? client))
        {
            string clientName = client.Name;
            client.OnClientChanged -= NotifyStoreChanged;
            client.Disconnect();
            client = null;
            
            _logger.LogInformation("Removed WifredClient with name: `{name}` and uid: {uid}", clientName, id);
            NotifyStoreChanged();
        }
        else
        {
            _logger.LogInformation("Failed to remove WifredClient with uid: {uid}", id);
        }
    }

    public void AddClient(WifredClient client)
    {
        client.OnClientChanged += NotifyStoreChanged;
        _clients[client.Id] = client;
        NotifyStoreChanged();
    }

    public IEnumerable<WifredClient> GetAllClients() => _clients.Values;
}
