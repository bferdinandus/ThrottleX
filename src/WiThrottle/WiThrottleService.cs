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

    private readonly WifredClientStore _clientStore;

    private ServiceDiscovery _serviceDiscovery = null!;
    private TcpListener _tcpListener = null!;

    private volatile bool _acceptingConnections = true;

    public WiThrottleService(WifredClientStore clientStore, ILogger<WiThrottleService> logger, IOptions<WiThrottleOptions> options, ILoggerFactory loggerFactory)
    {
        _clientStore = clientStore;

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
            _logger.LogInformation("Networkinterface with name: {x} found. Advertising bonjour only on that interface.", _options.NetworkInterfaceName);
            ipAddresses = foundNetworkInterface.GetIPProperties().UnicastAddresses.Select(uc => uc.Address);
        }
        else
        {
            _logger.LogInformation("Advertising bonjour only on all network interfaces.");
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
        while (!stoppingToken.IsCancellationRequested && _acceptingConnections)
        {
            try
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
                if (_acceptingConnections)
                {
                    _ = HandleClientAsync(tcpClient, stoppingToken)
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogCritical(t.Exception, "Unhandled fatal error in {name}", nameof(HandleClientAsync));
                            }
                        }, TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    tcpClient.Close(); // Reject connection if shutdown is underway
                }
            }
            catch (SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogInformation("AcceptTcpClientAsync aborted due to shutdown.");
                break;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogInformation("TCP listener disposed during shutdown.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AcceptTcpClientAsync.");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Handling WifredClient connection...");
            var customTcpClient = new CustomTcpClient(tcpClient, _loggerFactory.CreateLogger<CustomTcpClient>());

            await customTcpClient.SendMessageAsync("VN2.0");
            await customTcpClient.SendMessageAsync("*60");

            string? name = null;
            WiThrottleCommand command = WiThrottleMessageProcessor.ParseCommand(await customTcpClient.ReadNextMessageAsync(stoppingToken));
            _logger.LogInformation("Message received: {command}", command);
            if (command.Type == CommandType.Name)
            {
                name = command.Message;
            }

            string? uid = null;
            command = WiThrottleMessageProcessor.ParseCommand(await customTcpClient.ReadNextMessageAsync(stoppingToken));
            _logger.LogInformation("Message received: {command}", command);
            if (command.Type == CommandType.Uid)
            {
                uid = command.Message;
            }

            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(name))
            {
                _logger.LogError("Did not receive a name or id.");
                customTcpClient.Dispose();

                return;
            }

            WifredClient wifredClient = _clientStore.GetOrCreate(uid, name);
            wifredClient.UpdateConnection(customTcpClient);
            _ = wifredClient.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("WifredClient connected {name}/{id}", name, uid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling WifredClient connection.");
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WiThrottle stopping....");

        _acceptingConnections = false;

        _serviceDiscovery.Unadvertise();
        _serviceDiscovery.Dispose();

        // asynchronously disconnect all present and connected clients 
        var disconnectTasks = _clientStore.GetAllClients()
            .Where(client => client.IsConnected)
            .Select(client => Task.Run(client.Disconnect, cancellationToken));
        await Task.WhenAll(disconnectTasks);

        _tcpListener.Stop();

        _logger.LogInformation("WiThrottleService stopped....");
        await base.StopAsync(cancellationToken);
    }
}
