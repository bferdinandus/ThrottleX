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

    private readonly ConcurrentDictionary<TcpClient, string> _clients = new();

    public WiThrottleService(WiThrottleLocoTables locoTables, ILogger<WiThrottleService> logger, IOptions<WiThrottleOptions> options)
    {
        _locoTables = locoTables;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TcpListener listener = new(IPAddress.Any, _options.Port);
        listener.Start();
        _logger.LogInformation("Server started on port: {port}.", _options.Port);

        // Advertise the service using mDNS
        ServiceProfile serviceProfile = new("Fremo WiThrottle", "_withrottle._tcp", _options.Port);
        ServiceDiscovery serviceDiscovery = new();
        serviceDiscovery.Advertise(serviceProfile);

        while (!stoppingToken.IsCancellationRequested) {
            if (listener.Pending()) {
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                _clients.TryAdd(client, string.Empty);
                _logger.LogInformation("Client connected: {remoteEndPoint}", client.Client.RemoteEndPoint);
                _ = HandleClientAsync(client, stoppingToken);
            }

            await Task.Delay(100, stoppingToken); // Prevents tight loop if no clients are connecting
        }

        _logger.LogInformation("Server stopping....");
        serviceDiscovery.Unadvertise();
        serviceDiscovery.Dispose();
        listener.Stop();
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        // TODO: add client management for multiple clients

        byte[] buffer = new byte[1024];
        NetworkStream stream = client.GetStream();
        string? endPoint = client.Client.RemoteEndPoint?.ToString();

        try {
            // Send welcome message to the client
            await SendMessageAsync(WelcomeMessage(), stream, stoppingToken);

            while (client.Connected && !stoppingToken.IsCancellationRequested) {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                if (byteCount == 0) {
                    break;
                }

                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);
                _logger.LogDebug("Received from {endPoint}: {message}", endPoint, message);
                _clients[client] += message;

                // Process each line of the message
                // todo: before splitting make sure the last char is a NewLine. If not; put the last part back in _clients[client]
                string[] lines = _clients[client].Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines) {
                    _logger.LogInformation("Processing message from {endPoint}: {line}", endPoint, line);
                    await HandleIncomingMessageAsync(line, stream, stoppingToken);
                }

                // Reset the message buffer for the client
                _clients[client] = string.Empty;
            }
        } catch (Exception ex) {
            _logger.LogInformation("Error with client {endPoint}: {message}", endPoint, ex.Message);
        } finally {
            _clients.TryRemove(client, out _);
            client.Close();
            _logger.LogInformation("Client disconnected: {endPoint}", endPoint);
        }
    }

    private static string WelcomeMessage()
    {
        StringBuilder sb = new();
        sb.AppendLine("VN2.0");

        //sb.AppendLine("RL0");
        //sb.AppendLine("PPA2");
        //sb.AppendLine("RCC0");
        //sb.AppendLine($"PW5000");
        //sb.AppendLine("*10");

        return sb.ToString();
    }

    private async Task HandleIncomingMessageAsync(string message, NetworkStream stream, CancellationToken stoppingToken)
    {
        char command = message[0];

        switch (command) {
            case 'N': // Device Name
                string deviceName = message[1..];
                _logger.LogInformation("Received Name: {deviceName}", deviceName);

                await SendMessageAsync($"*0{Environment.NewLine}", stream, stoppingToken);

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                switch (subCommand) {
                    case 'U': // Identifier
                        string deviceIdentifier = message[2..];
                        _logger.LogInformation("Received Name: {deviceIdentifier}", deviceIdentifier);
                        break;
                    default:
                        _logger.LogWarning("Unknown sub command: {command} in {message}", command, message);
                        break;
                }

                break;
            case 'M': // MultiThrottle
                char mtIdentifier = message[1];
                string mtCommand = message[2..];


                string[] commandParts = mtCommand.Split(Constants.Separator);
                string key = commandParts[0];
                string action = commandParts[0];

                if (action[0] == '+') {
                    LocoTable loco = new() {
                        MultiThrottleInstance = mtIdentifier,
                        LocomotiveKey = key[1..]
                    };

                    if (_locoTables.Locos.All(x => x.LocomotiveKey != key)) {
                        _locoTables.Locos.Add(loco);
                    }

                    string response = $"M{loco.MultiThrottleInstance}+{loco.LocomotiveKey}{Constants.Separator}{Environment.NewLine}{loco.ToString()}";
                    await SendMessageAsync(response, stream, stoppingToken);
                }

                break;
            default:
                _logger.LogWarning("Unknown command: {command} in {message}", command, message);
                break;
        }
    }

    private async Task SendMessageAsync(string message, NetworkStream stream, CancellationToken stoppingToken)
    {
        byte[] messageToSend = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(messageToSend, 0, messageToSend.Length, stoppingToken);
        _logger.LogInformation("Send message:\n{message}", message);
    }
}
