using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ThrottleX.Core.Pages;

public class LoconetConnections : PageModel
{
    private readonly ILogger<LoconetConnections> _logger;

    public LoconetConnections(ILogger<LoconetConnections> logger)
    {
        _logger = logger;
    }

    public void OnGet() {}
}
