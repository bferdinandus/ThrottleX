using Loconet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ThrottleX.Core.Loconet;

public class LoconetService : BackgroundService
{
    private readonly List<(LoconetClient client, LoconetSend send)> _connections = new ();
    private readonly ILogger _logger;
    private readonly LoconetOptions _options;

    /// <summary>
    /// Just until I learnt how to geht the one instance properly....
    /// </summary>
    public static LoconetService? Instance;

    public IEnumerable<(LoconetClient client, LoconetSend send)> Clients => _connections;

    public LoconetService(ILogger<LoconetService>? logger, LoconetOptions? options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Instance = this;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(DisposeClients);

        foreach (var opt in _options.Clients)
        {
            var client = new LoconetClient(opt.Host, opt.Port, _logger);
            var send = new LoconetSend(client);
            _connections.Add((client, send));
            client.Start();
            send.Start();
        }
        return Task.CompletedTask;
    }

    private void DisposeClients()
    {
        foreach (var connection in _connections)
        {
            connection.send.Dispose();
            connection.client.Dispose();
        }
    }
}
