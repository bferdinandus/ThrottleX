WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string? urls = builder.Configuration.GetValue<string>("applicationUrl");
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}

// Add services to the container.
builder.Services.AddRazorPages();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();