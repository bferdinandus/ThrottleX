using Loconet.Msg.Accessor;

namespace Unittests.Loconet.Msg.Accessor;

public class ByteMaskHelperTest
{
    [Fact]
    public void Test()
    {
        ByteMaskHelper.ThrowOnInvalidMask(1);
        ByteMaskHelper.ThrowOnInvalidMask(2);
        ByteMaskHelper.ThrowOnInvalidMask(4);
        ByteMaskHelper.ThrowOnInvalidMask(16);
        ByteMaskHelper.ThrowOnInvalidMask(32);
        ByteMaskHelper.ThrowOnInvalidMask(64);
        Assert.Throws<ArgumentException>(() => ByteMaskHelper.ThrowOnInvalidMask(128));
        Assert.Throws<ArgumentException>(() => ByteMaskHelper.ThrowOnInvalidMask(256));
        Assert.Throws<ArgumentException>(() => ByteMaskHelper.ThrowOnInvalidMask(255));
        Assert.Throws<ArgumentException>(() => ByteMaskHelper.ThrowOnInvalidMask(0));
        Assert.Throws<ArgumentException>(() => ByteMaskHelper.ThrowOnInvalidMask(3));
    }
}
