using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

public class WiFiConfig
{
    [XmlElement("Connected")]
    public ValueElement<int> Connected { get; set; } = new();

    [XmlElement("SSID")]
    public ValueElement<string> SSID { get; set; } = new();

    [XmlElement("signalStrength")]
    public ValueElement<int> SignalStrength { get; set; } = new();

    [XmlElement("macAdress")]
    public ValueElement<string> MacAdress { get; set; } = new();
}
