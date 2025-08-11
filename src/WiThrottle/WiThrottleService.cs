using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.LocoTable;

namespace WiThrottle;

public class WiThrottleService : BackgroundService
{
    private readonly IThrottle2Table _locoTable;
    private readonly ILogger<WiThrottleService> _logger;
    private readonly WiThrottleOptions _options;

    public ConcurrentDictionary<string, TcpClientConnection> Clients { get; } = new();
    private ServiceDiscovery _serviceDiscovery = default!;
    private TcpListener _tcpListener = default!;

    public WiThrottleService(IThrottle2Table locoTable, ILogger<WiThrottleService> logger, IOptions<WiThrottleOptions> options)
    {
        _locoTable = locoTable;
        _logger = logger;
        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // open the port for wiFreds to connect on
        _tcpListener = new TcpListener(IPAddress.Any, _options.Port);

        _tcpListener.Start();
        _logger.LogInformation("Server started on port: {port}.", _options.Port);

        // Advertise the service using mDNS / zeroConf
        /*var mdns = new MulticastService();


        List<ServiceProfile> serviceProfiles = MulticastService.GetIPAddresses()
            .Where(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork)
            .Select(ipAddress => new ServiceProfile("Fremo WiThrottle", "_withrottle._tcp", _options.Port, [ipAddress]))
            .ToList();*/

        var foundNetworkInterface = MulticastService
            .GetNetworkInterfaces()
            .FirstOrDefault(i => i.Name.Equals(_options.NetworkInterfaceName, StringComparison.OrdinalIgnoreCase));

        IEnumerable<IPAddress> ipAddresses = null!;
        if (foundNetworkInterface is not null)
        {
            ipAddresses = foundNetworkInterface.GetIPProperties().UnicastAddresses.Select(uc => uc.Address);
        }

        ServiceProfile serviceProfile = new("Fremo WiThrottle", "_withrottle._tcp", _options.Port, ipAddresses);
        serviceProfile.AddProperty("thisIsKey", "thisIsValue");
        _serviceDiscovery = new ServiceDiscovery();
        _serviceDiscovery.Advertise(serviceProfile);
        _serviceDiscovery.Announce(serviceProfile);


        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
            TcpClientConnection clientConnection = new(_logger, _locoTable, tcpClient, stoppingToken);

            Clients.TryAdd(clientConnection.ClientId, clientConnection);
            _logger.LogInformation("Client connected: {remoteEndPoint}", tcpClient.Client.RemoteEndPoint);
            _ = Task.Run(() => ClientTask(clientConnection), stoppingToken);
        }
    }

    private async Task ClientTask(TcpClientConnection clientConnection)
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
            bool clientRemoveSuccess = Clients.TryRemove(clientConnection.ClientId, out _);
            if (!clientRemoveSuccess)
            {
                _logger.LogWarning("Client removal {client} unsuccessful", clientConnection.Name);
            }

            clientConnection.TcpClient.Close();
            _logger.LogInformation("Client disconnected: {client}", clientConnection.Name);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WiThrottle stopping....");
        _serviceDiscovery.Unadvertise();
        _serviceDiscovery.Dispose();
        _tcpListener.Stop();

        foreach (TcpClientConnection client in Clients.Values)
        {
            client.TcpClient.Close();
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("WiThrottle stopped....");
    }
}
