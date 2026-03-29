using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public class ImmichFrameAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string? _authenticationSecret;

    public ImmichFrameAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServerSettings settings)
        : base(options, logger, encoder)
    {
        _authenticationSecret = settings.GeneralSettings.AuthenticationSecret;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!RequiresAuthentication())
        {
            return Task.FromResult(CreateSuccessResult("anonymous"));
        }

        if (TryGetBearerToken(Request.Headers.Authorization.ToString(), out var token) &&
            string.Equals(token, _authenticationSecret, StringComparison.Ordinal))
        {
            return Task.FromResult(CreateSuccessResult("authenticatedUser"));
        }

        if (string.IsNullOrWhiteSpace(Request.Headers.Authorization))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        return Task.FromResult(AuthenticateResult.Fail("The AuthenticationSecret was not correct!"));
    }

    private bool RequiresAuthentication()
    {
        var endpoint = Context.GetEndpoint();
        var authorizeAttribute = endpoint?.Metadata?.GetMetadata<IAuthorizeData>();
        return _authenticationSecret != null && authorizeAttribute != null;
    }

    private static bool TryGetBearerToken(string? authorizationHeader, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorizationHeader["Bearer ".Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private AuthenticateResult CreateSuccessResult(string nameIdentifier)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, nameIdentifier) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
