using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using PineAI.UI.Components;
using PineAI.UI.Services;
using PineAI.Shared.Tools;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("AppName", "PineAI.UI")
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"]));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Build a scoped ApiClientService with a JWT-expiration delegating handler.
// Lazy<T> is used for AuthStateService and NavigationManager to break the
// circular dependency: AuthStateService → ApiClientService → AuthStateService.
// The Lazy wrappers resolve the services only when the first HTTP request is made,
// at which point both services are already fully constructed.
builder.Services.AddScoped<ApiClientService>(sp =>
{
    var lazyAuth = new Lazy<AuthStateService>(() => sp.GetRequiredService<AuthStateService>());
    var lazyNav  = new Lazy<NavigationManager>(() => sp.GetRequiredService<NavigationManager>());
    var baseUrl  = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5161/";

    var handler = new JwtExpirationHandler(lazyAuth, lazyNav)
    {
        InnerHandler = new HttpClientHandler()
    };
    var httpClient = new HttpClient(handler) 
    { 
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(30) // Prevent hanging requests
    };
    return new ApiClientService(httpClient);
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateService>());
builder.Services.AddScoped<MenuAccessStateService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<ExcelDownloadTokenStore>();

var businessSettings = builder.Configuration.GetSection("Business").Get<PineAI.UI.Services.BusinessSettings>() ?? new PineAI.UI.Services.BusinessSettings();
builder.Services.AddSingleton(businessSettings);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/download/customers-excel/{token}", (string token, ExcelDownloadTokenStore store) =>
{
    var phones = store.Consume(token);
    if (phones is null) return Results.NotFound();
    var bytes = XlsxBuilder.BuildPhoneNumbers(phones);
    var fileName = $"customers_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
