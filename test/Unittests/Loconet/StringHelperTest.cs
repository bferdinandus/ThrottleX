using Loconet;

namespace Unittests.Loconet;

public class StringHelperTest
{
    private enum E
    {
        Zero = 1,
        Two = 4,
        Eight = 256
    }

    [Fact]
    public void TestDisplayBits()
    {
        Assert.Equal("Zero(True), Two(False), Eight(True)", StringHelper.DisplayBits<E>(257));
        Assert.Equal("Zero(False), Two(True), Eight(False), EXTRA-BITS(0x2)", StringHelper.DisplayBits<E>(6));
    }
}
