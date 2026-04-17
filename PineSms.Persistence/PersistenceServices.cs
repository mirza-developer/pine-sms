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
        services.AddDbContext<PineSmsDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<ICustomerService, CustomerRepository>();
        services.AddScoped<ISmsService, SmsRepository>();
    }
}
