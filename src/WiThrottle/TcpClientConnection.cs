using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Sockets;
using System.Text;

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

    public async Task SendMessageAsync(string message)
    {
        byte[] messageToSend = Encoding.UTF8.GetBytes(message + Environment.NewLine);
        await _stream.WriteAsync(messageToSend, _stoppingToken);
        _logger.LogInformation("Sent to {Name}: '{message}'", Name, message);
    }

    internal async Task<int> ReadAsync(byte[] buffer)
    {
        return await _stream.ReadAsync(buffer, _stoppingToken);
    }
}
