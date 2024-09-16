using Bogus;
using Hydro;

namespace ThrottleX.Core.Pages.Components;

public class WiFredClients : HydroComponent
{
    public string TestString { get; set; } = "Hello World";
    Faker<WiFredClient> _wiFredClientFaker;
    public List<WiFredClient> Clients { get; set; } = [];

    public WiFredClients()
    {
        _wiFredClientFaker = new Faker<WiFredClient>()
            .RuleFor(x => x.Name, x => x.Name.FirstName())
            .RuleFor(x => x.Uid, x => x.UniqueIndex.ToString())
            .RuleFor(x => x.MacAddress, x => x.Internet.Mac())
            .RuleFor(x => x.Locos, x => x.Random.Int(0,4))
            .RuleFor(x => x.Status, x => x.PickRandom<WiFredStatus>())
            .RuleFor(x => x.TimeSinceLastMessage, x => x.Random.String2(5).ToString())
            ;
        Clients = _wiFredClientFaker.Generate(3);
    }

    public void Add()
    {
        TestString = $"{TestString}.";
    }
}


public class WiFredClient()
{
    public string Name { get; init; }
    public string Uid { get; init; }
    public string MacAddress { get; init; }
    public int Locos { get; init; }
    public WiFredStatus Status { get; init; }
    public string TimeSinceLastMessage { get; init; }


}

public enum WiFredStatus
{
    Online,
    Offline
}
