using Bogus;
using Hydro;

namespace ThrottleX.Core.Pages.Components;

public class WiFredClients : HydroComponent
{
    public string TestString { get; set; } = "Hello World";
    private Faker<WiFredClient> _wiFredClientFaker;
    public List<WiFredClient> Clients { get; init; }

    public WiFredClients()
    {
        _wiFredClientFaker = new Faker<WiFredClient>()
            .RuleFor(x => x.Name, x => x.Name.FirstName())
            .RuleFor(x => x.Uid, x => x.UniqueIndex.ToString())
            .RuleFor(x => x.MacAddress, x => x.Internet.Mac())
            .RuleFor(x => x.Locos, x => x.Random.Int(0,4))
            .RuleFor(x => x.Status, x => x.PickRandom<WiFredStatus>())
            .RuleFor(x => x.TimeSinceLastMessage, x => x.Date.RecentTimeOnly().ToString())
            ;
        Clients = _wiFredClientFaker.Generate(3);
    }

    public void Add()
    {
        TestString = $"{TestString}.";
    }
}


public class WiFredClient
{
    public string Name { get; init; } = string.Empty;
    public string Uid { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public int Locos { get; init; }
    public WiFredStatus Status { get; init; }
    public string TimeSinceLastMessage { get; init; } = string.Empty;


}

public enum WiFredStatus
{
    Online,
    Offline
}
