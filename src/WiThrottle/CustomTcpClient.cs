using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace WiThrottle;

public class CustomTcpClient : IDisposable
{
    private bool _isListening;
    private Task _listeningTask = null!;
    private readonly TcpClient _tcpClient;
    private readonly ILogger<CustomTcpClient> _logger;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<string> _messageChannel = Channel.CreateUnbounded<string>();

    public CustomTcpClient(TcpClient tcpClient, ILogger<CustomTcpClient> logger)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        var stream = _tcpClient.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
        StartListening();
        
        _logger.LogDebug("TcpClient connected");
    }

    private void StartListening()
    {
        _isListening = true;
        _listeningTask = Task.Run(async () =>
        {
            while (_isListening && _tcpClient.Connected)
            {
                var message = await _reader.ReadLineAsync(_cts.Token);
                if (message == null) continue;

                // a line was received, put it in the messageCannel so the rest of the code can read it
                _logger.LogDebug("TcpClient put message in channel: {message}", message);
                await _messageChannel.Writer.WriteAsync(message);
            }
        },  _cts.Token);
    }

    public async Task<string> ReadNextMessageAsync(CancellationToken stoppingToken)
    {
        return await _messageChannel.Reader.ReadAsync(stoppingToken);
    }

    public async Task SendMessageAsync(string message)
    {
        if (_tcpClient.Connected)
        {
            await _writer.WriteLineAsync(message);
        }
    }
    
    public string? GetIpAddress() => (_tcpClient.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();

    public bool IsConnected => _tcpClient.Connected;

    public void Dispose()
    {
        try
        {
            _logger.LogDebug("Disposing TcpClient");
            _isListening = false;
            _cts.Cancel();
            try
            {
                _listeningTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException _)
            {
                // do nothing, this error is expected
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Listening task ended with error");
            }
            
            _reader.Dispose();
            _writer.Dispose();
            _tcpClient.Dispose();
            _cts.Dispose();
            GC.SuppressFinalize(this);
            _logger.LogDebug("TcpClient closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disposing TcpClient");
        }
    }
}
