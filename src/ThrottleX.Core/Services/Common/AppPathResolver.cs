namespace ThrottleX.Core.Services.Common;

public static class AppPathResolver
{
    public static string GetLogDirectory(IHostEnvironment env) => !env.IsDevelopment() && OperatingSystem.IsLinux() ? "/var/log/throttle-x" : Path.Combine(env.ContentRootPath, "logs");

    public static string GetDataDirectory(IHostEnvironment env) => !env.IsDevelopment() && OperatingSystem.IsLinux() ? "/var/lib/throttle-x" : Path.Combine(env.ContentRootPath, "data");

    public static string GetCustomSettingsFilePath(IHostEnvironment env) => Path.Combine(GetConfigDirectory(env), "appsettings.custom.json");

    private static string GetConfigDirectory(IHostEnvironment env) => !env.IsDevelopment() && OperatingSystem.IsLinux() ? "/etc/throttle-x" : env.ContentRootPath;
}
