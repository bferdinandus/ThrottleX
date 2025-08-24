using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace WiThrottle;

public class WifredClient
{
    private readonly ILogger _logger;
    public string Id { get; private set; }
    public string Name { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    private CustomTcpClient? _client;

    public WifredClient(string id, string name, ILogger<WifredClient> logger)
    {
        _logger = logger;
        Id = id;
        Name = name;
        ConnectedAt = DateTime.UtcNow;
    }

    public async Task StartProcessingAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && IsConnected)
        {
            string? line = await _client.ReadNextMessageAsync(stoppingToken);

            if (line == null) continue;

            WiThrottleMessage? message = WiThrottleMessageProcessor.HandleMessage(line);
            _logger.LogInformation("Message received: {message}", message);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (message.Type)
            {
                case CommandType.Quit:
                    await WaitForDeviceDisconnectAsync(stoppingToken);
                    break;
                case CommandType.KeepAlive:
                    break;
                case CommandType.Unknown:
                    break;
            }
        }
    }

    public void UpdateConnection(CustomTcpClient client)
    {
        _client = client;

        ConnectedAt = DateTime.UtcNow;
    }

    public bool IsConnected => _client?.IsConnected ?? false;
    public string GetIpAddress()
    {
        return _client?.GetIpAddress() ??  string.Empty;
    }

    private async Task WaitForDeviceDisconnectAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        // Clean up if still connected
        if (IsConnected)
        {
            _logger.LogDebug("Cleaning up TCP client after timeout.");
            _client?.Dispose(); // Clean up the TCP client
            _client = null!;
        }
    }
}
