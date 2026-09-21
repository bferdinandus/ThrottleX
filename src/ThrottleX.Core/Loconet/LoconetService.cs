using Loconet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.LocoTable;
using ThrottleX.Core.LocoTable;

namespace ThrottleX.Core.Loconet;

public class LoconetService : BackgroundService
{
    private readonly List<(LoconetClient client, LoconetSend send)> _connections = new ();
    private readonly ILogger _logger;
    private readonly LoconetOptions _options;
    private readonly ILoconet2Table _locoTable;

    /// <summary>
    /// Just until I learnt how to geht the one instance properly....
    /// </summary>
    public static LoconetService? Instance;

    public event Action? OnConnectionsChanged;

    public void NotifyConnectionsChanged()
    {
        OnConnectionsChanged?.Invoke();
    }

    public IEnumerable<(LoconetClient client, LoconetSend send)> Clients => _connections;

    public int ClientCount => _connections.Count;

    public LoconetService(ILogger<LoconetService>? logger, ILoconet2Table locoTable, LoconetOptions? options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _locoTable = locoTable;
        Instance = this;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(DisposeClients);

        for (int index=0; index<_options.Clients.Count; index++)
        {
            var opt = _options.Clients[index];
            var mirror = new CommandStationMirror(_logger);

            var client = new LoconetClient(index, opt.Host, opt.Port, _logger);
            client.OnMessageReceived += mirror.OnMessage;
            client.OnStateChanged += NotifyConnectionsChanged;

            var send = new LoconetSend(client, mirror, _locoTable);
            send.OnStateChanged += NotifyConnectionsChanged;

            _connections.Add((client, send));
            client.Start();
            send.Start();
        }
        NotifyConnectionsChanged();
        return Task.CompletedTask;
    }

    private void DisposeClients()
    {
        foreach (var connection in _connections)
        {
            connection.send.OnStateChanged -= NotifyConnectionsChanged;
            connection.client.OnStateChanged -= NotifyConnectionsChanged;
            connection.send.Dispose();
            connection.client.Dispose();
        }
        NotifyConnectionsChanged();
    }
}
