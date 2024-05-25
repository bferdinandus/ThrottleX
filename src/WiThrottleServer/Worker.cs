using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WiThrottleServer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private TcpListener _listener;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, 12090);
        _listener.Start();
        _logger.LogInformation("WiThrottle Server started, listening on port 12090");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
                _logger.LogInformation("Handled new client...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listening for connections");
        }
        finally
        {
            _listener.Stop();
        }

        /*while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }*/
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using (client)
        {
            byte[] buffer = new byte[1024];
            NetworkStream stream = client.GetStream();

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