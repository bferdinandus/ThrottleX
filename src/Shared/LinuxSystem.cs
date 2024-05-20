using System.Diagnostics;

namespace Shared;

public static class LinuxSystem
{
    public static string GetFreeDiskSpace()
    {
        ProcessStartInfo info = new()
        {
            FileName = "/bin/bash",
            Arguments = "-c \"df -h / | grep / | awk '{print $4}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process? process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    public static string GetFreeMemory()
    {
        ProcessStartInfo info = new()
        {
            FileName = "/bin/bash",
            Arguments = "-c \"free -h --mega| grep Mem | awk '{print $7}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process? process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    public static string GetCpuUsage()
    {
        ProcessStartInfo info = new()
        {
            FileName = "/bin/bash",
            Arguments = "-c \"LC_ALL=C top -bn1 | awk '/^%Cpu/ {print 100 - $8\\\"%\\\" }'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process? process = Process.Start(info);

        return process?.StandardOutput.ReadToEnd().Trim() ?? "-";
    }

    public static string GetCpuTemperature()
    {
        ProcessStartInfo info = new()
        {
            FileName = "/bin/bash",
            Arguments = "-c \"vcgencmd measure_temp | egrep -o '[0-9]*\\.[0-9]*'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process? process = Process.Start(info);

        return $"{process?.StandardOutput.ReadToEnd().Trim()} °C";
    }
}