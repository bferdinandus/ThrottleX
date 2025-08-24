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
    private readonly ILogger<WiThrottleService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WiThrottleOptions _options;

    private readonly WifredDeviceStore _deviceStore;
    private readonly IThrottle2Table _locoTable;

    private ServiceDiscovery _serviceDiscovery = null!;
    private TcpListener _tcpListener = null!;

    public WiThrottleService(IThrottle2Table locoTable, WifredDeviceStore deviceStore, ILogger<WiThrottleService> logger, IOptions<WiThrottleOptions> options, ILoggerFactory loggerFactory)
    {
        _locoTable = locoTable;
        _deviceStore = deviceStore;

        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // open the port for wiFreds to connect on
        _tcpListener = new TcpListener(IPAddress.Any, _options.Port);
        _tcpListener.Start();
        _logger.LogInformation("Server started on port: {port}.", _options.Port);

        // Advertise the service using mDNS / zeroConf
        IEnumerable<IPAddress> ipAddresses = null!;

        // Try and get the ip-addresses of the network interface identified by the name from the config
        // if not found then ipAddresses will stay NULL and bonjour broadcast will happen on all active devices
        var foundNetworkInterface = MulticastService
            .GetNetworkInterfaces()
            .FirstOrDefault(i => i.Name.Equals(_options.NetworkInterfaceName, StringComparison.OrdinalIgnoreCase));
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
            var tcpClient = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
            _ = Task.Run(() => HandleClientAsync(tcpClient, stoppingToken), stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Handling client connection...");
            var customTcpClient = new CustomTcpClient(tcpClient, _loggerFactory.CreateLogger<CustomTcpClient>());

            await customTcpClient.WriteLineAsync("VN2.0");
            await customTcpClient.WriteLineAsync("*60");

            string? name = null;
            WiThrottleMessage? message = WiThrottleMessageProcessor.HandleMessage(await customTcpClient.ReadNextMessageAsync(stoppingToken));
            _logger.LogInformation("Message received: {message}", message);
            if (message?.Type == CommandType.Name)
            {
                name = message.Message;
            }

            string? id = null;
            message = WiThrottleMessageProcessor.HandleMessage(await customTcpClient.ReadNextMessageAsync(stoppingToken));
            _logger.LogInformation("Message received: {message}", message);
            if (message?.Type == CommandType.Uid)
            {
                id = message.Message;
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                _logger.LogError("Did not receive a name or id.");
                tcpClient.Close();

                return;
            }

            var wifredClient = _deviceStore.GetOrCreate(id, name);
            wifredClient.UpdateConnection(customTcpClient);
            _ = wifredClient.StartProcessingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling client connection.");
        }
    }

    /*TcpClientConnection clientConnection = new(_logger, _locoTable, tcpClient, stoppingToken);
    Clients.TryAdd(clientConnection.ClientId, clientConnection);
    _logger.LogInformation("Client connected: {remoteEndPoint}", tcpClient.Client.RemoteEndPoint);
    _ = Task.Run(() => ClientTask(clientConnection), stoppingToken);*/

    /*private async Task ClientTask(TcpClientConnection clientConnection)
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
    }*/

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WiThrottle stopping....");
        _serviceDiscovery.Unadvertise();
        _serviceDiscovery.Dispose();
        _tcpListener.Stop();

        /*foreach (TcpClientConnection client in Clients.Values)
        {
            client.TcpClient.Close();
        }*/

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("WiThrottle stopped....");
    }
}
