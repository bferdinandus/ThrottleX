using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Models;

namespace WiThrottle;

public class WiThrottleService : BackgroundService
{
    private readonly WiThrottleLocoTables _locoTables;
    private readonly ILogger<WiThrottleService> _logger;
    private readonly WiThrottleOptions _options;

    private readonly ConcurrentDictionary<string, TcpClientConnection> _clients = new();
    private ServiceDiscovery _serviceDiscovery = default!;
    private TcpListener _tcpListener = default!;

    public WiThrottleService(WiThrottleLocoTables locoTables, ILogger<WiThrottleService> logger, IOptions<WiThrottleOptions> options)
    {
        _locoTables = locoTables;
        _logger = logger;
        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // open the port for wiFreds to connect on
        _tcpListener = new TcpListener(IPAddress.Any, _options.Port);

        _tcpListener.Start();
        _logger.LogInformation("Server started on port: {port}.", _options.Port);

        // Advertise the service using mDNS / zefoConf
        ServiceProfile serviceProfile = new("Fremo WiThrottle", "_withrottle._tcp", _options.Port);
        serviceProfile.AddProperty("thisIsKey", "thisIsValue");
        _serviceDiscovery = new ServiceDiscovery();
        _serviceDiscovery.Advertise(serviceProfile);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) 
        {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
            TcpClientConnection clientConnection = new(_logger, tcpClient, stoppingToken);

            _clients.TryAdd(clientConnection.ClientId, clientConnection);
            _logger.LogInformation("Client connected: {remoteEndPoint}", tcpClient.Client.RemoteEndPoint);
            _ = Task.Run(() => ClientTask(clientConnection), stoppingToken);
        }
    }

    public async Task ClientTask(TcpClientConnection clientConnection)
    {
        try
        {
            await clientConnection.HandleClientAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error with client {client}", clientConnection.Name);
        }
        finally
        {
            _clients.TryRemove(clientConnection.ClientId, out _);
            clientConnection.Client.Close();
            _logger.LogInformation("Client disconnected: {client}", clientConnection.Name);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WiThrottle stopping....");
        _serviceDiscovery.Unadvertise();
        _serviceDiscovery.Dispose();
        _tcpListener.Stop();

        foreach (TcpClientConnection client in _clients.Values) {
            client.Client.Close();
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("WiThrottle stopped....");
    }
}
