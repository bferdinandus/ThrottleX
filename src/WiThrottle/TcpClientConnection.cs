using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Shared.LocoTable;

namespace WiThrottle;

public class TcpClientConnection
{
    public TcpClient Client { get; }
    public string ClientId { get; } = Guid.NewGuid().ToString();
    public DateTime ConnectionTime { get; } = DateTime.Now;
    public string Name { get; internal set; }

    private readonly ILogger<WiThrottleService> _logger;
    private readonly CancellationToken _stoppingToken;
    private readonly Stream _stream;

    public TcpClientConnection(ILogger<WiThrottleService> logger, TcpClient client, CancellationToken stoppingToken)
    {
        _logger = logger;
        Client = client;
        _stoppingToken = stoppingToken;

        _stream = Client.GetStream();
        Name = ClientId; // will be overwritten when client tells us its name
    }

    internal async Task HandleClientAsync()
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new();

        // Send welcome message to the client
        await SendMessageAsync("VN2.0");

        int bytesRead;
        while ((bytesRead = await ReadAsync(buffer)) != 0)
        {
            // Convert the received bytes into a string and append to the message buffer
            string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            messageBuffer.Append(receivedText);

            _logger.LogDebug("Received text from {client}: {receivedText}", Name, receivedText);

            // Extract and process complete messages
            List<string> messages = ExtractCompleteMessages(ref messageBuffer);
            foreach (string message in messages)
            {
                _logger.LogInformation("Processing message from {client}: `{message}`", Name, message);
                await HandleIncomingMessageAsync(message);
            }
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
        if (!lastMessageComplete)
        {
            messageBuffer.Append(splitMessages[^1]);
        }

        return messages;
    }

    private async Task HandleIncomingMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        char command = message[0];

        switch (command)
        {
            case 'N': // Device Name
                string deviceName = message[1..];
                _logger.LogInformation("Received Name: {deviceName}", deviceName);
                Name = deviceName;

                await SendMessageAsync("*60");

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                switch (subCommand)
                {
                    case 'U': // Identifier
                        string deviceIdentifier = message[2..];
                        _logger.LogInformation("Received Uid: {deviceIdentifier}", deviceIdentifier);
                        break;
                    default:
                        _logger.LogWarning("Unknown sub command: {command} in {message}", command, message);
                        break;
                }

                break;
            case 'M':
                await MultiThrottleAsync(message);
                break;
            case '*':
                break;
            default:
                _logger.LogWarning("Unknown command: {command} in {message}", command, message);
                break;
        }
    }

    private async Task MultiThrottleAsync(string message)
    {
        char mtIdentifier = message[1];
        string mtCommand = message[2..];

        string[] commandParts = mtCommand.Split(Constants.Separator);
        string first = commandParts[0];

        if (first[0] == '+')
        {
            var address = first[1..].ParseWtAddress();

            var locoRow = LocoTableImpl.Instance.GetRowForAddress(address);

            _logger.LogInformation($"{Name}: got loco row with state {locoRow.LocoRowState} for address {locoRow.Address}");

//            string response = $"M{loco.MultiThrottleInstance}+{loco.LocomotiveKey}{Constants.Separator}";
//            await SendMessageAsync(response);
        }
    }

    private async Task SendMessageAsync(string message)
    {
        byte[] messageToSend = Encoding.UTF8.GetBytes(message + Environment.NewLine);
        await _stream.WriteAsync(messageToSend, _stoppingToken);
        _logger.LogInformation("Sent to {Name}: '{message}'", Name, message);
    }

    private async Task<int> ReadAsync(byte[] buffer)
    {
        return await _stream.ReadAsync(buffer, _stoppingToken);
    }
}
