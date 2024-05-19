using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

public class IndexModel : PageModel
{
    public string FreeDiskSpace { get; set; } = string.Empty;
    public string FreeMemory { get; set; } = string.Empty;
    public string CpuUsage { get; set; } = string.Empty;
    public string CpuTemperature { get; set; } = string.Empty;

    public void OnGet()
    {
        FreeDiskSpace = GetFreeDiskSpace();
        FreeMemory = GetFreeMemory();
        CpuUsage = GetCpuUsage();
        CpuTemperature = GetCpuTemperature();
    }

    private string GetFreeDiskSpace()
    {
        var info = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-c \"df -h / | grep / | awk '{print $4}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    private string GetFreeMemory()
    {
        var info = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-c \"free -h --mega| grep Mem | awk '{print $7}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    private string GetCpuUsage()
    {
        var info = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-c \"LC_ALL=C top -bn1 | awk '/^%Cpu/ {print 100 - $8\\\"%\\\" }'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    private string GetCpuTemperature()
    {
        var info = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-c \"vcgencmd measure_temp | egrep -o '[0-9]*\\.[0-9]*'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var process = Process.Start(info);

        return $"{process?.StandardOutput.ReadToEnd().Trim()} °C";
    }
}
