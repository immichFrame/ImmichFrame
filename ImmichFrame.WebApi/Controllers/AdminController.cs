using ImmichFrame.Core.Exceptions;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    public class AdminStatusDto
    {
        /// <summary>What the admin UI should show.</summary>
        public AdminUiState State { get; set; } = AdminUiState.Disabled;
    }

    public class AdminSetupDto
    {
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class SettingsUpdateResultDto
    {
        public List<string> Warnings { get; set; } = new();
    }

    public class AccountTestResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = ImmichFrameAdminAuthenticationHandler.SchemeName)]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly SettingsService _settingsService;
        private readonly AdminAuthService _adminAuthService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(ILogger<AdminController> logger, SettingsService settingsService,
            AdminAuthService adminAuthService, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _settingsService = settingsService;
            _adminAuthService = adminAuthService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Status", Name = "GetAdminStatus")]
        [AllowAnonymous]
        public AdminStatusDto GetStatus()
        {
            return new AdminStatusDto { State = _adminAuthService.State };
        }

        /// <summary>
        /// Claims an unconfigured instance by setting the admin password. Anonymous on
        /// purpose: without it a fresh install has no way into the admin UI. Closes for
        /// good as soon as a password exists, from either the environment or the database.
        /// Any settings imported from a config file are preserved.
        /// </summary>
        [HttpPost("Setup", Name = "SetupAdmin")]
        [AllowAnonymous]
        public async Task<ActionResult<SettingsUpdateResultDto>> Setup([FromBody] AdminSetupDto setup)
        {
            if (!_adminAuthService.SetupRequired)
            {
                return Problem(detail: "ImmichFrame is already configured.", statusCode: StatusCodes.Status409Conflict);
            }

            if (string.IsNullOrWhiteSpace(setup.AdminPassword))
            {
                return Problem(detail: "An admin password is required.", statusCode: StatusCodes.Status400BadRequest);
            }

            var settings = _settingsService.GetRawSettings();
            settings.GeneralSettingsImpl ??= new GeneralSettings();
            settings.GeneralSettingsImpl.AdminPassword = setup.AdminPassword;

            try
            {
                await _settingsService.UpdateAsync(settings);
            }
            catch (SettingsNotValidException ex)
            {
                _logger.LogWarning("Rejected setup: {message}", ex.Message);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation("Admin password set through onboarding.");
            return Ok(new SettingsUpdateResultDto());
        }

        /// <summary>
        /// The raw settings for editing. Secrets are included on purpose: the caller is
        /// authenticated with the dedicated admin password, and masking would break the
        /// GET-edit-PUT round-trip.
        /// </summary>
        [HttpGet("Settings", Name = "GetAdminSettings")]
        public ServerSettings GetSettings()
        {
            return _settingsService.GetRawSettings();
        }

        [HttpPut("Settings", Name = "UpdateAdminSettings")]
        public async Task<ActionResult<SettingsUpdateResultDto>> UpdateSettings([FromBody] ServerSettings settings)
        {
            Core.Interfaces.IServerSettings validated;
            try
            {
                validated = await _settingsService.UpdateAsync(settings);
            }
            catch (SettingsNotValidException ex)
            {
                _logger.LogWarning("Rejected settings update: {message}", ex.Message);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }

            // Non-blocking checks: saving is allowed even if a server is currently unreachable
            // (e.g. pre-provisioning), but the admin should know about it.
            var checks = await Task.WhenAll(validated.Accounts
                .Select(account => ImmichServerVersionChecker.CheckAccount(account, _httpClientFactory)));

            return Ok(new SettingsUpdateResultDto
            {
                Warnings = checks.Where(c => !c.Success).Select(c => c.Message).ToList()
            });
        }

        [HttpPost("Settings/TestAccount", Name = "TestAccount")]
        public async Task<AccountTestResultDto> TestAccount([FromBody] ServerAccountSettings account)
        {
            try
            {
                account.ValidateAndInitialize(); // resolves ApiKeyFile if given
            }
            catch (Exception ex)
            {
                return new AccountTestResultDto { Success = false, Message = ex.Message };
            }

            var check = await ImmichServerVersionChecker.CheckAccount(account, _httpClientFactory);
            return new AccountTestResultDto { Success = check.Success, Message = check.Message, Version = check.Version };
        }
    }
}
