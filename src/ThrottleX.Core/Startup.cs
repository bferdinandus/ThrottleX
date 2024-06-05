using Loconet;
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
        // Configure and add WiThrottleService
        services.Configure<WiThrottleOptions>(_configuration.GetSection("WiThrottle"));
        services.AddHostedService<WiThrottleService>();
        services.AddHostedService<LoconetService>();

        // Add services to the container.

        services.AddRazorPages();

        // Add other services here
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

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(routeBuilder => { routeBuilder.MapRazorPages(); });
    }
}
