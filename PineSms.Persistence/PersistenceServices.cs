using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineSms.Core.Contracts;
using PineSms.Persistence.Repositories;
using PineSms.Persistence.Services;
using PineSms.Persistence.Services.Messaging;
using PineSms.Persistence.Workers;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace PineSms.Persistence;

public static class PersistenceServices
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connStr = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContextPool<PineSmsDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connStr);
            else
                options.UseSqlServer(connStr);
        });

        services.AddMemoryCache();

        var redisConn = configuration.GetConnectionString("Redis");
        var fusionBuilder = services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromDays(1),
                DistributedCacheDuration = TimeSpan.FromDays(14)
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer());

        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConn);
            fusionBuilder
                .WithRegisteredDistributedCache()
                .WithStackExchangeRedisBackplane(opts => opts.Configuration = redisConn);
        }

        services.AddScoped<ICustomerService, CustomerRepository>();
        // Register SmsRepository as both the interface and concrete type so the worker can resolve it
        services.AddScoped<SmsRepository>();
        services.AddScoped<ISmsService>(sp => sp.GetRequiredService<SmsRepository>());

        services.AddScoped<IOrderService, OrderRepository>();
        services.AddScoped<IApiKeyService, ApiKeyRepository>();
        services.AddScoped<IBotConversationService, BotConversationRepository>();
        services.AddScoped<IMenuLinkService, MenuLinkRepository>();

        services.AddScoped<IBaleMessengerService, BaleMessengerService>();

        //services.AddHostedService<ScheduledSmsWorker>();
    }

    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PineSmsDbContext>();
        if (db.Database.IsSqlite())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }
}
