using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WiThrottle;

public class WifredDeviceStore
{
    private readonly ILogger<WifredDeviceStore> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, WifredClient> _clients = new();

    public WifredDeviceStore(ILogger<WifredDeviceStore> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    public WifredClient GetOrCreate(string id, string name)
    {
        return _clients.GetOrAdd(id, _ =>
        {
            var newClient = new WifredClient(id, name, _loggerFactory.CreateLogger<WifredClient>());
            _logger.LogInformation("Created new WifredClient with name: `{name}` and id: {id}", name, id);
            
            return newClient;
        });
    }

    public WifredClient? GetClient(string id)
    {
        _clients.TryGetValue(id, out var client);
        return client;
    }

    public void AddClient(WifredClient client)
    {
        _clients[client.Id] = client;
    }

    public IEnumerable<WifredClient> GetAllClients() => _clients.Values;
}
