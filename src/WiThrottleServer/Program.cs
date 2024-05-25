using WiThrottleServer;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Logging.ClearProviders().AddConsole();

IHost host = builder.Build();
host.Run();