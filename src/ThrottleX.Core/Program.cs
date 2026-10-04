using Serilog;
using ThrottleX.Core;
using ThrottleX.Core.Extensions;
using ThrottleX.Core.Services.Common;

CreateHostBuilder(args).Build().Run();
return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            string customConfigPath = AppPathResolver.GetCustomSettingsFilePath(context.HostingEnvironment);

            // Add custom configuration layer
            config.AddJsonFile(customConfigPath, optional: true, reloadOnChange: true);
        })
        .UseSerilog(ConfigSerilog)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();

            webBuilder.UseKestrel(serverOptions => { serverOptions.ListenAnyIP(5000); });
        });

static void ConfigSerilog(HostBuilderContext context, LoggerConfiguration configuration)
{
    string logDir = AppPathResolver.GetLogDirectory(context.HostingEnvironment);

    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFromLoggingConfiguration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine(logDir, "ThrottleX-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] (Thread {ThreadId}) {Message:lj}{NewLine}{Exception}");
}
