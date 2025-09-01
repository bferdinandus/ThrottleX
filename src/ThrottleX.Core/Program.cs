using Serilog;
using ThrottleX.Core;
using static Serilog.Events.LogEventLevel;

CreateHostBuilder(args).Build().Run();
return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog(ConfigSerilog)
        .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

static void ConfigSerilog(HostBuilderContext context, LoggerConfiguration configuration)
{
    configuration.MinimumLevel.Verbose()
        .Enrich.WithThreadId()
        .WriteTo.Console(Debug, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {ThreadId}: [{SourceContext}] [{RequestId}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
        .WriteTo.File("Logs/ThrottleX-.log",
            Verbose,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] [{Level:u3}] {ThreadId}: [{SourceContext}] [{RequestId}] {Message:lj}{NewLine}{Exception}");
}
