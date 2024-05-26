using System.Net.Sockets;
using System.Text;

namespace WiThrottleServer;
public class Client
{
    public Guid Id { get; init; }
    public TcpClient TcpClient { get; init; }
    public StringBuilder MessageBuffer { get; init; }

    public string Name { get; set; } = string.Empty;
    public string UniqueIdentifier { get; set; } = string.Empty;

    public Client(TcpClient tcpClient)
    {
        Id = Guid.NewGuid();
        TcpClient = tcpClient;
        MessageBuffer = new StringBuilder();
    }
}