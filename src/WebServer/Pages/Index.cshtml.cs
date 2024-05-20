using Microsoft.AspNetCore.Mvc.RazorPages;

using Shared;

namespace WebServer.Pages;

public class IndexModel : PageModel
{
    public string FreeDiskSpace { get; set; } = string.Empty;
    public string FreeMemory { get; set; } = string.Empty;
    public string CpuUsage { get; set; } = string.Empty;
    public string CpuTemperature { get; set; } = string.Empty;

    public void OnGet()
    {
        FreeDiskSpace = LinuxSystem.GetFreeDiskSpace();
        FreeMemory = LinuxSystem.GetFreeMemory();
        CpuUsage = LinuxSystem.GetCpuUsage();
        CpuTemperature = LinuxSystem.GetCpuTemperature();
    }
}