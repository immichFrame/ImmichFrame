using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ImmichFrame.WebApi.Helpers;

public class ImmichFrameAdminAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ImmichFrameAdminScheme";

    private readonly AdminAuthService _adminAuthService;

    public ImmichFrameAdminAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AdminAuthService adminAuthService)
        : base(options, logger, encoder)
    {
        _adminAuthService = adminAuthService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.FromResult(AuthenticateResult.Success(CreateTicket("anonymous")));
        }

        if (!_adminAuthService.AdminEnabled)
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "ImmichFrame is not set up yet. Open /admin to choose an admin password, "
                + $"or set the {AdminAuthService.AdminPasswordEnvVar} environment variable."));
        }

        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (!_adminAuthService.ValidatePassword(token))
        {
            return Task.FromResult(AuthenticateResult.Fail("The admin password was not correct!"));
        }

        return Task.FromResult(AuthenticateResult.Success(CreateTicket("admin")));
    }

    private AuthenticationTicket CreateTicket(string name)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, name) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
    }
}
