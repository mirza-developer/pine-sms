using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineAI.Core.Contracts;
using PineAI.Identity.Extensions;
using PineAI.Identity.Models;
using PineAI.Identity.Services;

namespace PineAI.Identity;

public static class IdentityServices
{
    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connStr = configuration.GetConnectionString("IdentityConnection")!;

        services.AddDbContext<PineAIIdentityContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connStr);
            else
                options.UseSqlServer(connStr);
        });

        services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<PineAIIdentityContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddPineAIAuthorize(configuration);
    }
}

