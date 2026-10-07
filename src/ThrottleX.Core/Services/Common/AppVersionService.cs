using System.Reflection;

namespace ThrottleX.Core.Services.Common;

public class AppVersionService : IAppVersionService
{
    public string DisplayVersion => $"v{field}";

    public AppVersionService()
    {
        var informationalVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        DisplayVersion = informationalVersion?.Split('+')[0] ?? "0.0.0";
    }
}
