using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

[XmlRoot("wiFred")]
public class WiFredConfig
{
    [XmlElement("structurVersion")]
    public ValueElement<int> StructurVersion { get; set; } = new();

    [XmlElement("throttleName")]
    public ValueElement<string> ThrottleName { get; set; } = new();

    [XmlElement("localIP")]
    public ValueElement<string> LocalIP { get; set; } = new();

    [XmlElement("firmwareRevision")]
    public ValueElement<string> FirmwareRevision { get; set; } = new();

    [XmlElement("batteryVoltage")]
    public ValueElement<int> BatteryVoltage { get; set; } = new();

    [XmlElement("batteryLow")]
    public ValueElement<bool> BatteryLow { get; set; } = new();

    [XmlElement("WiFi")]
    public WiFiConfig WiFi { get; set; } = new();

    [XmlArray("LOCOS")]
    [XmlArrayItem("LOCO")]
    public List<LocoConfig> Locos { get; set; } = new();

    [XmlArray("NETWORKS")]
    [XmlArrayItem("NETWORK")]
    public List<NetworkConfig> Networks { get; set; } = new();

    [XmlElement("LOCOSERVER")]
    public LocoServerConfig LocoServer { get; set; } = new();

    [XmlElement("centerSwitch")]
    public ValueElement<int> CenterSwitch { get; set; } = new();
}

public class ValueElement<T>
{
    [XmlAttribute("value")]
    public T Value { get; set; } = default!;
}
