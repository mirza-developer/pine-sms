using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using PineSms.Api.Auth;
using PineSms.Api.Queue;
using PineSms.Api.Swagger;
using PineSms.Api.Workers;
using PineSms.Core;
using PineSms.Core.Middlewares;
using PineSms.Identity;
using PineSms.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("AppName", "PineSms.Api")
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"]));

builder.Services.AddControllers();
builder.Services.AddCoreServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSingleton<OrderNotifyQueue>();
builder.Services.AddHostedService<OrderNotifyWorker>();
builder.Services.AddHttpClient("Melipayamak", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Melipayamak:BaseUrl"]);
});
builder.Services.AddHttpClient("BaleMessenger", client =>
{
    var url = builder.Configuration["BaleMessenger:SafirBaseUrl"];
    if (!string.IsNullOrEmpty(url))
        client.BaseAddress = new Uri(url);
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PineSms API", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key for order notification endpoint"
    });

    c.OperationFilter<ApiKeyOperationFilter>();
});

var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<AppExceptionHandlerMiddleware>();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

