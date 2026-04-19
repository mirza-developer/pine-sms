using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineSms.Core.Contracts;
using PineSms.Persistence.Repositories;
using PineSms.Persistence.Services;

namespace PineSms.Persistence;

public static class PersistenceServices
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connStr = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<PineSmsDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connStr);
            else
                options.UseSqlServer(connStr);
        });

        services.AddScoped<ICustomerService, CustomerRepository>();
        services.AddScoped<ISmsService, SmsRepository>();
    }
}
