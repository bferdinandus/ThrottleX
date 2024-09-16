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
        while (!stoppingToken.IsCancellationRequested) {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
            TcpClientConnection clientConnection = new() { Client = tcpClient };

            _clients.TryAdd(clientConnection.ClientId, clientConnection);
            _logger.LogInformation("Client connected: {remoteEndPoint}", tcpClient.Client.RemoteEndPoint);
            _ = Task.Run(() => HandleClientAsync(clientConnection, stoppingToken), stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClientConnection clientConnection, CancellationToken stoppingToken)
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new();

        try {
            await using NetworkStream stream = clientConnection.Client.GetStream();

            // Send welcome message to the client
            await SendMessageAsync(WelcomeMessage(), stream, stoppingToken);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, stoppingToken)) != 0) {
                // Convert the received bytes into a string and append to the message buffer
                string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(receivedText);

                _logger.LogDebug("Received text from {clientId}: {receivedText}", clientConnection.ClientId, receivedText);

                // Extract and process complete messages
                List<string> messages = ExtractCompleteMessages(ref messageBuffer);
                foreach (string message in messages) {
                    _logger.LogInformation("Processing message from {clientId}: `{message}`", clientConnection.ClientId, message);
                    await HandleIncomingMessageAsync(message, stream, stoppingToken);
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Error with client {clientId}", clientConnection.ClientId);
        } finally {
            _clients.TryRemove(clientConnection.ClientId, out _);
            clientConnection.Client.Close();
            _logger.LogInformation("Client disconnected: {clientId}", clientConnection.ClientId);
        }
    }

    private static List<string> ExtractCompleteMessages(ref StringBuilder messageBuffer)
    {
        string bufferContent = messageBuffer.ToString();
        List<string> messages = [];

        // when the buffer content ends with a newline the last message is considered complete
        // if not complete it should be added back to the messageBuffer and wait for more characters
        bool lastMessageComplete = bufferContent[^1] == '\r' || bufferContent[^1] == '\n';

        // Split the buffer content by newline characters
        string[] splitMessages = bufferContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Add all complete messages to the list
        messages.AddRange(lastMessageComplete ? splitMessages : splitMessages[..^1]);

        // Clear the messageBuffer and append the last (incomplete) part back to it
        messageBuffer.Clear();
        if (!lastMessageComplete) {
            messageBuffer.Append(splitMessages[^1]);
        }

        return messages;
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
        if (string.IsNullOrWhiteSpace(message)) {
            return;
        }

        char command = message[0];

        switch (command) {
            case 'N': // Device Name
                string deviceName = message[1..];
                _logger.LogInformation("Received Name: {deviceName}", deviceName);

                await SendMessageAsync($"*60{Environment.NewLine}", stream, stoppingToken);

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                switch (subCommand) {
                    case 'U': // Identifier
                        string deviceIdentifier = message[2..];
                        _logger.LogInformation("Received Uid: {deviceIdentifier}", deviceIdentifier);
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

                    string response = $"M{loco.MultiThrottleInstance}+{loco.LocomotiveKey}{Constants.Separator}{Environment.NewLine}";
                    await SendMessageAsync(response, stream, stoppingToken);
                }

                break;
            case '*':
                break;
            default:
                _logger.LogWarning("Unknown command: {command} in {message}", command, message);
                break;
        }
    }

    private async Task SendMessageAsync(string message, NetworkStream stream, CancellationToken stoppingToken)
    {
        byte[] messageToSend = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(messageToSend, stoppingToken);
        _logger.LogInformation("Send message: {message}", message);
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
