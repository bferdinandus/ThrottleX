using Hydro.Configuration;
using Serilog;
using Shared.LocoTable;
using ThrottleX.Core.Loconet;
using ThrottleX.Core.LocoTable;
using WiThrottle;

namespace ThrottleX.Core;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Configure and add Loconet related services
        services.AddSingleton<LocoTable.LocoTable>();
        services.AddSingleton<ILoconet2Table>(sp => sp.GetRequiredService<LocoTable.LocoTable>());

        // Configure and add MultiThrottle related services
        services.AddSingleton<IThrottle2Table>(sp => sp.GetRequiredService<LocoTable.LocoTable>());

        services.Configure<LoconetOptions>(_configuration.GetSection("Loconet"));
        services.AddSingleton<LoconetService>();

        services.AddHostedService(sp => sp.GetRequiredService<LoconetService>());

        // Configure and add WiThrottle related services
        services.AddSingleton<WifredClientStore>();
        services.Configure<WiThrottleOptions>(_configuration.GetSection("WiThrottle"));
        
        services.AddSingleton<WiThrottleService>();
        services.AddHostedService(p => p.GetRequiredService<WiThrottleService>());
        
        // Configure and add website services
        services.AddRazorPages();
        services.AddHydro();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        } else {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(routeBuilder => { routeBuilder.MapRazorPages(); });
        app.UseHydro(env); // Hydro
    }
}
