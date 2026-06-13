using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

public class LocoConfig
{
    [XmlAttribute("ID")]
    public int Id { get; set; }

    [XmlElement("DCCadress")]
    public ValueElement<int> DccAddress { get; set; } = new();

    [XmlElement("Direction")]
    public ValueElement<int> Direction { get; set; } = new();

    [XmlElement("LongAdress")]
    public ValueElement<int> LongAddress { get; set; } = new();

    [XmlArray("FUNCTIONS")]
    [XmlArrayItem("Function")]
    public List<LocoFunctionConfig> Functions { get; set; } = new();
}
