using System.Xml.Serialization;
using WiThrottle.Models.WiFred;
using Xunit;

namespace Unittests;

public class WiFredConfigTest
{
    [Fact]
    public void TestMalformedXmlDeclaration()
    {
        var xmlContent = "<?XML version=\"1.0\" encoding=\"UTF-8\"?><wiFred><batteryVoltage value=\"100\"/></wiFred>";
        var serializer = new XmlSerializer(typeof(WiFredConfig));
        using var stringReader = new StringReader(xmlContent);

        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(stringReader));
    }

    [Fact]
    public void TestFixedXmlDeclaration()
    {
        var xmlContent = "<?XML version=\"1.0\" encoding=\"UTF-8\"?><wiFred><batteryVoltage value=\"100\"/></wiFred>";
        
        if (xmlContent.StartsWith("<?XML", StringComparison.OrdinalIgnoreCase))
        {
            xmlContent = "<?xml" + xmlContent.Substring(5);
        }
        
        var serializer = new XmlSerializer(typeof(WiFredConfig));
        using var stringReader = new StringReader(xmlContent);

        var config = (WiFredConfig?)serializer.Deserialize(stringReader);
        Assert.NotNull(config);
        Assert.Equal(100, config!.BatteryVoltage.Value);
    }
}
