using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PineAI.OrderTrack;
using PineAI.OrderTrack.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var apiKey = builder.Configuration["ApiKey"] ?? string.Empty;

builder.Services.AddScoped(sp =>
{
    var client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    return client;
});

builder.Services.AddScoped<OrderTrackingService>();

var businessSettings = builder.Configuration.GetSection("Business").Get<BusinessSettings>() ?? new BusinessSettings();
builder.Services.AddSingleton(businessSettings);

await builder.Build().RunAsync();
