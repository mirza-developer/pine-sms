using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PineAI.Identity.Utilities;

namespace PineAI.Identity.Extensions;

public static class AuthorizationExtension
{
    public static void AddPineAIAuthorize(this IServiceCollection services, IConfiguration configuration)
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
                IssuerSigningKey = CryptographyTools.GetSymmetricKey(string.IsNullOrEmpty(configSec["Signing"]) ? "PineAI_JWT_Secret_Key_32Chars" : configSec["Signing"]!),
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
