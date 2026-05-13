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

    if (!Environment.UserInteractive)
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

    builder.Services.AddSerilog((services, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("AppName", "PineSms.BaleBot")
        .WriteTo.Console()
        .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]));

    var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;

    builder.Services.AddDbContext<PineSmsDbContext>(options =>
    {
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            options.UseSqlite(connStr);
        else
            options.UseSqlServer(connStr);
    });

    var token = builder.Configuration["BaleMessenger:Token"] ?? string.Empty;
    builder.Services.AddHttpClient("BaleBotClient", client =>
    {
        var baseUrl = builder.Configuration["BaleMessenger:BaseUrl"];
        if (!baseUrl.EndsWith('/')) baseUrl += '/';
        client.BaseAddress = new Uri($"{baseUrl}bot{token}/");
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    var aiProvider = builder.Configuration["AiProvider"] ?? "github";
    if (aiProvider.Equals("arvan", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHttpClient("ArvanAiClient");
        builder.Services.AddSingleton<IChatAgentService, ArvanChatAgentService>();
    }
    else
        builder.Services.AddSingleton<IChatAgentService, ChatAgentService>();
    builder.Services.AddSingleton<ChatSessionStore>();
    builder.Services.AddSingleton<BaleBotClient>();
    builder.Services.AddScoped<IBotUpdateHandler, BotUpdateHandler>();

    builder.Services.AddHostedService<BaleBotWorker>();

    var host = builder.Build();

    var agentService = host.Services.GetRequiredService<IChatAgentService>();
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
