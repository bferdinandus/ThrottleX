using System.Xml.Serialization;

namespace WiThrottle.Models.WiFred;

public class LocoFunctionConfig
{
    [XmlAttribute("ID")]
    public int Id { get; set; }

    [XmlAttribute("value")]
    public int Value { get; set; }
}
