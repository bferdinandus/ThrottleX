using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Makaretu.Dns;

namespace WiThrottleServer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private TcpListener _listener;
    private ServiceDiscovery _sd;
    private ConcurrentDictionary<Guid, Client> _clients = new();
    private ushort _port = 12080;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _port = 12090;
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _logger.LogInformation("WiThrottle Server started, listening on port {port}", _port);

        // Advertise the service using mDNS
        ServiceProfile service = new("WiThrottleServer", "_withrottle._tcp", _port);
        service.AddProperty("someKey", "someValue");
        _sd = new ServiceDiscovery();
        _sd.Advertise(service);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientConnectionAsync(client, stoppingToken);
                _logger.LogInformation("Handled new client...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listening for connections");
        }
        finally
        {
            _sd.Dispose();
            _listener.Stop();
        }
    }

    private async Task HandleClientConnectionAsync(TcpClient tcpClient, CancellationToken stoppingToken)
    {
        Client client = new Client(tcpClient);
        _clients[client.Id] = client;
        _logger.LogInformation("Client connected: {ClientId}", client.Id);

        // Send a welcome message to the newly connected client
        await SendWelcomeMessageAsync(client, stoppingToken);

        _ = HandleClientAsync(client, stoppingToken);
    }

    private async Task SendWelcomeMessageAsync(Client client, CancellationToken stoppingToken)
    {
        NetworkStream stream = client.TcpClient.GetStream();
        StringBuilder sb = new();
        sb.AppendLine("VN2.0");
        sb.AppendLine("RL0");
        sb.AppendLine("PPA2");
        sb.AppendLine("RCC0");
        sb.AppendLine("PW12080");
        sb.AppendLine("*0");
        string welcomeMessage = sb.ToString();
        byte[] welcomeMessageBytes = Encoding.ASCII.GetBytes(welcomeMessage);

        await stream.WriteAsync(welcomeMessageBytes, stoppingToken);
        _logger.LogInformation("Sent welcome message to {ClientId}\n{WelcomeMessage}", client.Id, welcomeMessage);
    }

    private async Task HandleClientAsync(Client client, CancellationToken stoppingToken)
    {
        using (client.TcpClient)
        {
            byte[] buffer = new byte[1024];
            NetworkStream stream = client.TcpClient.GetStream();

            while (!stoppingToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, stoppingToken);
                if (bytesRead == 0)
                {
                    _logger.LogInformation("Client closed connection.");
                    break; // client closed connection
                }

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                _logger.LogInformation("Received message: {Message}", message);

                // Handle the message (this is where you can add your custom logic)
            }
        }
    }
}

