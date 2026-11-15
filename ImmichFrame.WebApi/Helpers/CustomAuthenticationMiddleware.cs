using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

public class CustomAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public CustomAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(GetScheme(context));

        if (!result.Succeeded)
        {
            // File mode fails admin authentication on purpose. That is not a missing
            // password (401): the admin API is turned off (403), even with a bearer token.
            var fileManaged = GetScheme(context) == ImmichFrameAdminAuthenticationHandler.SchemeName
                && context.RequestServices.GetService<AdminAuthService>()?.State == AdminUiState.File;
            context.Response.StatusCode = fileManaged
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(result.Failure?.Message ?? "Unauthorized");
            return;
        }

        await _next(context);
    }

    private static string GetScheme(HttpContext context)
    {
        // Admin endpoints declare their scheme via [Authorize(AuthenticationSchemes = ...)];
        // everything else keeps the original client scheme behavior.
        var authorizeData = context.GetEndpoint()?.Metadata?.GetMetadata<IAuthorizeData>();
        if (authorizeData?.AuthenticationSchemes?.Contains(ImmichFrameAdminAuthenticationHandler.SchemeName) == true)
        {
            return ImmichFrameAdminAuthenticationHandler.SchemeName;
        }

        return "ImmichFrameScheme";
    }
}
