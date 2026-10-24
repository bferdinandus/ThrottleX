using System.Xml.Serialization;
using WiThrottle;
using WiThrottle.Models.WiFred;

namespace Unittests;

public class WiFredConfigTest
{
    [Theory]
    [InlineData("WLFRED", WiFredXmlVariant.WlFred)]
    [InlineData("wiFred", WiFredXmlVariant.WiFred)]
    public void TestAlternateRootName(string rootName, WiFredXmlVariant expectedVariant)
    {
        var xmlContent = $"<{rootName} xmlns=\"\"><batteryVoltage value=\"100\"/></{rootName}>";
        var config = WifredClient.DeserializeConfig(xmlContent, out var detectedVariant);

        Assert.NotNull(config);
        Assert.Equal(100, config.BatteryVoltage.Value);
        Assert.Equal(expectedVariant, detectedVariant);
    }

    [Fact]
    public void TestActualWlFredXml()
    {
        var xmlContent = """
                         <?XML version="1.0" encoding="UTF-8"?>
                         <WLFRED>
                             <structurVersion value="1"/>
                             <throttleName value="WLFRED-Felix"/>
                             <localIP value="192.168.105.14"/>
                             <firmwareRevision value="2026_08_28_rev03_ini_WLFRED_4_8"/>
                             <batteryVoltage value="4109"/>
                             <batteryLow value="0"/>
                             <WiFi>
                                 <Connected value="1"/>
                                 <SSID value="BurgamStausee"/>
                                 <signalStrength value="-66"/>
                                 <macAdress value="ecda3be164e4"/>
                             </WiFi>
                             <LOCOS>
                                 <LOCO ID="1">
                                     <DCCadress value="-1"/>
                                     <Mode value=""/>
                                     <Direction value="0"/>
                                     <LongAdress value="1"/>
                                 </LOCO>
                             </LOCOS>
                             <NETWORKS>
                                 <NETWORK>
                                     <SSID value="BurgamStausee"/>
                                     <Enabled value="1"/>
                                 </NETWORK>
                             </NETWORKS>
                             <LOCOSERVER>
                                 <ServerName value="undef"/>
                                 <Port value="12090"/>
                                 <Automatic value="1"/>
                             </LOCOSERVER>
                             <centerSwitch value="-2"/>
                         </WLFRED>
                         """;

        var config = WifredClient.DeserializeConfig(xmlContent, out var detectedVariant);

        Assert.NotNull(config);
        Assert.Equal(WiFredXmlVariant.WlFred, detectedVariant);
        Assert.Equal(4109, config.BatteryVoltage.Value);
        Assert.Equal("WLFRED-Felix", config.ThrottleName.Value);
        Assert.Single(config.Locos);
        Assert.Equal("BurgamStausee", config.WiFi.SSID.Value);
    }

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
        Assert.Equal(100, config.BatteryVoltage.Value);
    }
}
