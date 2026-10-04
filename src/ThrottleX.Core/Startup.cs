using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Shared.LocoTable;
using ThrottleX.Core.Services.Common;
using ThrottleX.Core.Services.Loconet;
using ThrottleX.Core.Services.LocoTable;
using ThrottleX.Core.Services.SystemHealth;
using ThrottleX.Core.Services.SystemTime;
using WiThrottle;

namespace ThrottleX.Core;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public Startup(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Configure and add Loconet services
        services.AddSingleton<LocoTableImpl>();
        services.AddSingleton<ILoconet2Table>(sp => sp.GetService<LocoTableImpl>()!);
        services.AddSingleton<IThrottle2Table>(sp => sp.GetService<LocoTableImpl>()!);

        LoconetOptions? loconetConfig = _configuration.GetSection("Loconet").Get<LoconetOptions>();
        services.Configure<LoconetOptions>(_configuration.GetSection("Loconet"));

        services.AddSingleton<LoconetService>(sp =>
        {
            ILogger<LoconetService> loggerService = sp.GetRequiredService<ILogger<LoconetService>>();
            ILoconet2Table loconet2TableService = sp.GetRequiredService<ILoconet2Table>();

            return new LoconetService(loggerService, loconet2TableService, loconetConfig);
        });
        services.AddHostedService(sp => sp.GetRequiredService<LoconetService>());

        // Configure and add WiThrottle services
        services.AddHttpClient("WiFredClient", client => { client.Timeout = TimeSpan.FromSeconds(5); });
        services.AddSingleton<WifredClientStore>();
        services.Configure<WiThrottleOptions>(_configuration.GetSection("WiThrottle"));

        services.AddSingleton<WiThrottleService>();
        services.AddHostedService(p => p.GetRequiredService<WiThrottleService>());

        // Configure and add system health monitoring
        services.Configure<SystemHealthOptions>(_configuration.GetSection("SystemHealth"));
        services.AddSingleton<ISystemMetricsReader, LinuxSystemMetricsReader>();
        services.AddSingleton<SystemHealthService>();
        services.AddSingleton<ISystemHealthService>(sp => sp.GetRequiredService<SystemHealthService>());
        services.AddHostedService(sp => sp.GetRequiredService<SystemHealthService>());

        // Configure and add system time service
        services.Configure<SystemTimeOptions>(_configuration.GetSection("SystemTime"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISystemTimeService, SystemTimeService>();

        // Register App Version Service
        services.AddSingleton<IAppVersionService, AppVersionService>();

        // Configure and add website services
        IDataProtectionBuilder dataProtection = services.AddDataProtection();
        if (_environment.IsProduction())
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(AppPathResolver.GetDataDirectory(_environment)));
        }

        services.AddRazorPages();
        services.AddServerSideBlazor();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(routeBuilder =>
        {
            routeBuilder.MapBlazorHub();
            routeBuilder.MapFallbackToPage("/_Host");
        });
    }
}
