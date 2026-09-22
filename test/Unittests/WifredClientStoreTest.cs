using Microsoft.Extensions.Logging.Abstractions;
using Shared.LocoTable;
using WiThrottle;
using Xunit;

namespace Unittests;

public class WifredClientStoreTest
{
    private class DummyThrottleTable : IThrottle2Table
    {
        public int Count => 0;
        public IThrottle2Row GetRowForAddress(Shared.Models.IAddress address) => throw new NotImplementedException();
    }

    private class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    [Fact]
    public void StoreChanged_Fires_On_Add_And_Forget()
    {
        var store = new WifredClientStore(
            NullLogger<WifredClientStore>.Instance,
            NullLoggerFactory.Instance,
            new DummyThrottleTable(),
            new DummyHttpClientFactory());

        int storeEvents = 0;
        store.OnStoreChanged += () => storeEvents++;

        var client = store.GetOrCreate("test-uid", "TestThrottle");
        Assert.Equal(1, storeEvents);

        client.NotifyClientChanged();
        Assert.Equal(2, storeEvents);

        store.ForgetClient("test-uid");
        Assert.Equal(3, storeEvents);

        // Client changes after forgetting should no longer trigger store events
        client.NotifyClientChanged();
        Assert.Equal(3, storeEvents);
    }
}
