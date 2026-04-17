using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineSms.Core.Contracts;
using PineSms.Identity.Extensions;
using PineSms.Identity.Models;
using PineSms.Identity.Services;

namespace PineSms.Identity;

public static class IdentityServices
{
    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connStr = configuration.GetConnectionString("IdentityConnection")!;

        services.AddDbContext<PineSmsIdentityContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connStr);
            else
                options.UseSqlServer(connStr);
        });

        services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<PineSmsIdentityContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddPineSmsAuthorize(configuration);
    }
}

