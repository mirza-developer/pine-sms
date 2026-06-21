using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PineAI.Landing;
using PineAI.Landing.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? string.Empty;

builder.Services.AddScoped(sp =>
{
    var client = new HttpClient();
    if (!string.IsNullOrEmpty(apiBaseUrl))
        client.BaseAddress = new Uri(apiBaseUrl);
    return client;
});

builder.Services.AddScoped<ContactService>();

var siteSettings = builder.Configuration.GetSection("Site").Get<SiteSettings>() ?? new SiteSettings();
builder.Services.AddSingleton(siteSettings);

await builder.Build().RunAsync();
