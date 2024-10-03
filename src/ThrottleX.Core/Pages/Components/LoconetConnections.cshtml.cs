using Bogus;
using Hydro;
using Loconet;
using ThrottleX.Core.Loconet;

namespace ThrottleX.Core.Pages.Components;

public class LoconetConnections : HydroComponent
{
    public IEnumerable<(LoconetClient client, LoconetSend send)>? Connections => LoconetService.Instance?.Clients;
}
