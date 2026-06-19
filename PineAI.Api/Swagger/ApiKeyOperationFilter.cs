using Microsoft.OpenApi.Models;
using PineAI.Api.Auth;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PineAI.Api.Swagger;

/// <summary>
/// Adds the ApiKey security requirement to operations that use the ApiKey authentication scheme.
/// </summary>
public class ApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasApiKeyAuth = context.MethodInfo
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any(a => a.AuthenticationSchemes == ApiKeyAuthenticationHandler.SchemeName);

        if (!hasApiKeyAuth) return;

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };
    }
}
