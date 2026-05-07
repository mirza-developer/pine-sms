using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using PineSms.BaleBot.Services;
using PineSms.BaleBot.Workers;
using PineSms.Persistence.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
var builder = Host.CreateApplicationBuilder(args);

// Required for running as a Windows Service (handles SCM lifecycle, etc.)
builder.Services.AddWindowsService(options => options.ServiceName = "PineSms BaleBot");

// When hosted as a Windows Service the working directory defaults to System32;
// fix it to the directory where the executable lives so relative paths resolve correctly.
if (!Environment.UserInteractive)
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);

builder.Services.AddSerilog((services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

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
    var baseUrl = builder.Configuration["BaleMessenger:BaseUrl"];
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "BaleBot host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
