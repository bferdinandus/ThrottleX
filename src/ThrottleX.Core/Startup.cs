using Hydro.Configuration;
using Serilog;
using Shared.LocoTable;
using Shared.Models;
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
        // Add other services here
        services.AddSingleton<LocoTableImpl>();
        services.AddSingleton<ILoconet2Table>(sp => sp.GetService<LocoTableImpl>()!);
        services.AddSingleton<IThrottle2Table>(sp => sp.GetService<LocoTableImpl>()!);

        // Configure and add WiThrottleService
        services.Configure<WiThrottleOptions>(_configuration.GetSection("WiThrottle"));
        services.AddHostedService<WiThrottleService>();

        var loconetConfig = _configuration.GetSection("Loconet").Get<LoconetOptions>();
        //services.Configure<LoconetOptions>(_configuration.GetSection("Loconet"));
        services.AddHostedService(sp => new LoconetService(sp.GetService<ILogger<LoconetService>>(), sp.GetService<ILoconet2Table>()!, loconetConfig));

        // Add services to the container.

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

        //Add support to logging request with SERILOG
        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(routeBuilder => { routeBuilder.MapRazorPages(); });
        app.UseHydro(env); // Hydro
    }
}
