using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

public class NetworkConfig
{
    [XmlElement("SSID")]
    public ValueElement<string> SSID { get; set; } = new();

    [XmlElement("Key")]
    public ValueElement<string> Key { get; set; } = new();

    [XmlElement("Enabled")]
    public ValueElement<int> Enabled { get; set; } = new();
}
