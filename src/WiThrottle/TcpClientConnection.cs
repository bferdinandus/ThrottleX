using System.Net.Sockets;

namespace WiThrottle;

public class TcpClientConnection
{
    public TcpClient Client { get; init; } = null!;
    public string ClientId { get; } = Guid.NewGuid().ToString();
    public DateTime ConnectionTime { get; } = DateTime.Now;
}
