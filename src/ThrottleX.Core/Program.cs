using Serilog;
using ThrottleX.Core;
using ThrottleX.Core.Logging;

CreateHostBuilder(args).Build().Run();
return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog(ConfigSerilog)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();

            webBuilder.UseKestrel(serverOptions => { serverOptions.ListenAnyIP(5000); });
        });

static void ConfigSerilog(HostBuilderContext context, LoggerConfiguration configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFromLoggingConfiguration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/ThrottleX-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] (Thread {ThreadId}) {Message:lj}{NewLine}{Exception}");
