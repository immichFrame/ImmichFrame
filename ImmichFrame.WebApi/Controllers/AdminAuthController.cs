using System.Security.Claims;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminBasicAuthService _adminBasicAuthService;

    public AdminAuthController(IAdminBasicAuthService adminBasicAuthService)
    {
        _adminBasicAuthService = adminBasicAuthService;
    }

    [HttpGet("session")]
    [AllowAnonymous]
    public async Task<ActionResult<AdminAuthSessionDto>> GetSession()
    {
        var authenticationResult = await HttpContext.AuthenticateAsync(AuthSchemes.Admin);
        var principal = authenticationResult.Principal;

        return Ok(new AdminAuthSessionDto
        {
            IsConfigured = _adminBasicAuthService.HasUsers,
            IsAuthenticated = authenticationResult.Succeeded && principal?.Identity?.IsAuthenticated == true,
            Username = principal?.FindFirstValue(ClaimTypes.Name)
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AdminLogin")]
    public async Task<ActionResult<AdminAuthSessionDto>> Login([FromBody] AdminLoginRequest request)
    {
        if (!_adminBasicAuthService.HasUsers)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Admin login is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("A username and password are required.");
        }

        if (!_adminBasicAuthService.ValidateCredentials(request.Username, request.Password))
        {
            return Unauthorized("Invalid username or password.");
        }

        var credentialVersion = _adminBasicAuthService.GetCredentialVersion(request.Username);
        if (string.IsNullOrWhiteSpace(credentialVersion))
        {
            return Unauthorized("Invalid username or password.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, request.Username),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim("immichframe_admin_credential_version", credentialVersion)
        };
        var identity = new ClaimsIdentity(claims, AuthSchemes.Admin);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(AuthSchemes.Admin, principal);

        return Ok(new AdminAuthSessionDto
        {
            IsConfigured = true,
            IsAuthenticated = true,
            Username = request.Username
        });
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Admin)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Admin);
        return NoContent();
    }
}
