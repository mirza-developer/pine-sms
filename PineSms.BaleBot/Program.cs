using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PineSms.BaleBot.Services;
using PineSms.BaleBot.Workers;
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

// ── AI Agent ─────────────────────────────────────────────────────────────────
// ChatAgentService is a singleton: it holds the initialized AIAgent instance
// which is thread-safe after InitAsync is called once at startup.
builder.Services.AddSingleton<ChatAgentService>();
// ChatSessionStore is a singleton: it keeps per-user AI session JSON in memory.
builder.Services.AddSingleton<ChatSessionStore>();

// ── Bot services ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<BaleBotClient>();
builder.Services.AddScoped<IBotUpdateHandler, BotUpdateHandler>();

// ── Worker ────────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<BaleBotWorker>();

var host = builder.Build();

// Initialize the AI agent before starting the worker loop
var agentService = host.Services.GetRequiredService<ChatAgentService>();
await agentService.InitAsync();

await host.RunAsync();
