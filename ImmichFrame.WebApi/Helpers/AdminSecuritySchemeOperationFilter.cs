using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Documents the admin bearer scheme on the operations that actually require it. The
/// admin controller mixes authenticated and anonymous endpoints (Status and Setup must
/// stay reachable before a password exists), so the requirement is applied per operation
/// rather than globally.
/// </summary>
public class AdminSecuritySchemeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        if (method?.DeclaringType == null)
            return;

        var usesAdminScheme = method.DeclaringType.GetCustomAttributes(true)
            .Concat(method.GetCustomAttributes(true))
            .OfType<IAuthorizeData>()
            .Any(a => a.AuthenticationSchemes?.Contains(ImmichFrameAdminAuthenticationHandler.SchemeName) == true);

        var allowsAnonymous = method.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any();

        if (!usesAdminScheme || allowsAnonymous)
            return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ImmichFrameAdminAuthenticationHandler.SchemeName
                }
            }] = Array.Empty<string>()
        });

        // CustomAuthenticationMiddleware answers a failed admin authentication with the
        // plain-text reason, not a ProblemDetails body.
        operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse
        {
            Description = "Unauthorized",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["text/plain"] = new() { Schema = new OpenApiSchema { Type = "string" } }
            }
        });
    }
}
