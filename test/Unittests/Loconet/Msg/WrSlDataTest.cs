using Loconet.Msg;
using Loconet.Msg.Accessor;

namespace Unittests.Loconet.Msg;

public class WrSlDataTest
{
    [Fact]
    public void TestEnumerate()
    {
        var data = new WrSlData();
        Assert.StrictEqual(11, data.EnumerateFields.Count());
    }

    [Fact]
    public void TestWrSlData()
    {
        var uut = new MessageLookup();
        var result = uut.ParseMessage([0xef, 14, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130]);
        Assert.IsType<WrSlData>(result);
        var slot = (WrSlData)result;
        Assert.StrictEqual(20, slot.Slot.Value);
        Assert.StrictEqual(40, slot.Adr.Value);
        Assert.StrictEqual(90, slot.Adr2.Value);
        Assert.StrictEqual(30, slot.Stat.Value);
        Assert.StrictEqual(80, slot.SS2.Value);
        Assert.StrictEqual(50, slot.Spd.Value);
        Assert.StrictEqual(60, slot.Dirf.Value);
        Assert.StrictEqual(100, slot.Snd.Value);
        Assert.StrictEqual(110, slot.Id1.Value);
        Assert.StrictEqual(120, slot.Id2.Value);
        Assert.StrictEqual(70, slot.Trk.Value);
    }
}
