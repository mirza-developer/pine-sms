using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using PineSms.UI.Components;
using PineSms.UI.Services;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register a named HttpClient so we can resolve it inside a Scoped factory below.
// AddHttpClient<T> registers ApiClientService as Transient, which means each Blazor
// component injection gets a different instance than the one held by AuthStateService,
// so the JWT token set by AuthStateService would never reach the components.
// Using a named client + explicit Scoped registration ensures one shared instance per
// Blazor circuit (user connection), so SetToken() is visible to every component.
builder.Services.AddHttpClient("PineSmsApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5161/");
});
builder.Services.AddScoped<ApiClientService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("PineSmsApiClient");
    return new ApiClientService(httpClient);
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateService>());
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<ExcelDownloadTokenStore>();

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
