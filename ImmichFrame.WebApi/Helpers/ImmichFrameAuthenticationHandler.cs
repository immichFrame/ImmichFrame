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
        var endpoint = Context.GetEndpoint();
        var authorizeAttribute = endpoint?.Metadata?.GetMetadata<IAuthorizeData>();

        if (_authenticationSecret == null || authorizeAttribute == null)
        {
            // No auth is required
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "anonymous") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            if (token == _authenticationSecret)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "authenticatedUser") };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("The AuthenticationSecret was not correct!"));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
    }
}