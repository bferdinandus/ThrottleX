using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ThrottleX.Core.Pages;

public class WiFredClients : PageModel
{
    private readonly ILogger<WiFredClients> _logger;

    public WiFredClients(ILogger<WiFredClients> logger)
    {
        _logger = logger;
    }

    public void OnGet() {}
}
