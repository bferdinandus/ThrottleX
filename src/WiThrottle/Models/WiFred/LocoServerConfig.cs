using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

public class LocoServerConfig
{
    [XmlElement("ServerName")]
    public ValueElement<string> ServerName { get; set; } = new();

    [XmlElement("Port")]
    public ValueElement<int> Port { get; set; } = new();

    [XmlElement("Automatic")]
    public ValueElement<int> Automatic { get; set; } = new();
}
