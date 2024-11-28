using Serilog;
using ThrottleX.Core;

CreateHostBuilder(args).Build().Run();
return;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
        .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
