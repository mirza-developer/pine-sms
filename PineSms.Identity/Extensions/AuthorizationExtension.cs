using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineSms.Identity.Utilities;

namespace PineSms.Identity.Extensions;

public static class AuthorizationExtension
{
    public static void AddPineSmsAuthorize(this IServiceCollection services, IConfiguration configuration)
    {
        var configSec = configuration.GetSection("Identity");

        services.AddAuthorization();
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new()
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CryptographyTools.GetSymmetricKey(string.IsNullOrEmpty(configSec["Signing"]) ? "PineSms_JWT_Secret_Key_32Chars" : configSec["Signing"]!),
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = configSec["Audience"],
                ValidateIssuer = true,
                ValidIssuer = configSec["Issuer"]
            };
        });
    }
}
