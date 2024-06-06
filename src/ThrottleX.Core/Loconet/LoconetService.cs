using Loconet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ThrottleX.Core.Loconet;

public class LoconetService : BackgroundService
{
    private readonly List<LoconetClient> _clients = new ();
    private readonly ILogger _logger;
    private readonly LoconetOptions _options;

    public LoconetService(ILogger<LoconetService>? logger, LoconetOptions? options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var opt in _options.Clients)
        {
            LoconetClient client = new LoconetClient(opt.Host, opt.Port, _logger, stoppingToken);
            _clients.Add(client);
            client.Start();
        }
        return Task.CompletedTask;
    }
}
