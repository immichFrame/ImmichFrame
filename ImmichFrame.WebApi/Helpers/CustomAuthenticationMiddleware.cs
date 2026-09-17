using ImmichFrame.WebApi.Helpers;
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
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
