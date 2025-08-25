using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.LocoTable;

namespace WiThrottle;

public class WifredDeviceStore
{
    private readonly ILogger<WifredDeviceStore> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IThrottle2Table _locoTable;
    private readonly ConcurrentDictionary<string, WifredClient> _clients = new();

    public WifredDeviceStore(ILogger<WifredDeviceStore> logger, ILoggerFactory loggerFactory, IThrottle2Table locoTable)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _locoTable = locoTable;
    }
    public WifredClient GetOrCreate(string uid, string name)
    {
        return _clients.GetOrAdd(uid, _ =>
        {
            var newClient = new WifredClient(uid, name, _loggerFactory.CreateLogger<WifredClient>(), _locoTable);
            _logger.LogInformation("Created new WifredClient with name: `{name}` and uid: {uid}", name, uid);
            
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
