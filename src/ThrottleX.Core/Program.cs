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
                 .WriteTo.Console(Debug)
                 .WriteTo.File("Logs\\ThrottleX-.log",
                               Verbose,
                               rollingInterval: RollingInterval.Day,
                               outputTemplate: "{Timestamp:DDD.HH:mm:ss.fff} [{Level:u3}] {ThreadId}: {Message:lj}{NewLine}{Exception}");
}
