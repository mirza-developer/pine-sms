using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PineSms.BaleBot.Services;
using PineSms.BaleBot.Workers;
using PineSms.Persistence;
using PineSms.Persistence.Services;

var builder = Host.CreateApplicationBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<PineSmsDbContext>(options =>
{
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connStr);
    else
        options.UseSqlServer(connStr);
});

// ── Bale Bot HTTP client ──────────────────────────────────────────────────────
var token = builder.Configuration["BaleMessenger:Token"] ?? string.Empty;
builder.Services.AddHttpClient("BaleBotClient", client =>
{
    var baseUrl = builder.Configuration["BaleMessenger:BaseUrl"] ?? "https://tapi.bale.ai/";
    // Ensure base URL ends with a slash, then append the bot token path
    if (!baseUrl.EndsWith('/')) baseUrl += '/';
    client.BaseAddress = new Uri($"{baseUrl}bot{token}/");
    // Timeout must be longer than the long-poll timeout (30 s) to avoid premature cancellation
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ── Bot services ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<BaleBotClient>();
builder.Services.AddScoped<IBotUpdateHandler, BotUpdateHandler>();

// ── Worker ────────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<BaleBotWorker>();

var host = builder.Build();
await host.RunAsync();
