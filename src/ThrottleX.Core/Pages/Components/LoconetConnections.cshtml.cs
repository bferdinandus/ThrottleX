using Hydro;
using Loconet;
using ThrottleX.Core.Loconet;

namespace ThrottleX.Core.Pages.Components;

public class LoconetConnections : HydroComponent
{
    private readonly LoconetService _loconetService;
    public IEnumerable<(LoconetClient client, LoconetSend send)>? Connections => _loconetService.Clients;

    public LoconetConnections(LoconetService loconetService)
    {
        _loconetService = loconetService;
    }
}
