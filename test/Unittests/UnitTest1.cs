namespace Unittests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Test2(bool testParam)
    {
        Assert.True(testParam);
    }
}
