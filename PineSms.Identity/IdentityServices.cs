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
        services.AddDbContext<PineSmsIdentityContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("IdentityConnection"));
        });

        services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<PineSmsIdentityContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddPineSmsAuthorize(configuration);
    }
}
