using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet
{
    public class LoconetClient
    {
        private readonly string _host;
        private readonly ushort _port;
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellation;

        public LoconetClient(string host, ushort port, ILogger logger, CancellationToken cancellation)
        {
            _host = host;
            _port = port;
            _logger = logger;
            _cancellation = cancellation;
            cancellation.Register(Stop);
        }

        public void Start()
        {
            _logger.LogInformation($"Starting Loconet client for {_host}:{_port}");
        }

        public void Stop() 
        {
            _logger.LogInformation($"Stopping Loconet client for {_host}:{_port}");
        }
    }
}
