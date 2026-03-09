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
        services.AddSingleton<LocoTableImpl>();
        services.AddSingleton<ILoconet2Table>(sp => sp.GetService<LocoTableImpl>()!);
        services.AddSingleton<IThrottle2Table>(sp => sp.GetService<LocoTableImpl>()!);

        LoconetOptions? loconetConfig = _configuration.GetSection("Loconet").Get<LoconetOptions>();
        
        Console.WriteLine("Loconet config host: " + loconetConfig?.Clients.Select(c => c.Host).First());
        services.Configure<LoconetOptions>(_configuration.GetSection("Loconet"));
        
        services.AddHostedService(sp =>
        {
            var loggerService = sp.GetService<ILogger<LoconetService>>();
            Console.WriteLine("logger service:" + loggerService);
            var loconet2Tableservice =  sp.GetService<ILoconet2Table>();
            Console.WriteLine("loconet 2 table service: " + loconet2Tableservice);
            return new LoconetService(loggerService, loconet2Tableservice!, loconetConfig);
        });

        // Configure and add WiThrottle relates services
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
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(routeBuilder =>
        {
            routeBuilder.MapRazorPages();
        });
        app.UseHydro(env); // Hydro
    }
}
