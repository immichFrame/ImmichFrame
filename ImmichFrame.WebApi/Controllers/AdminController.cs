using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
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

    public class AdminAlbumDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AssetCount { get; set; }
        public bool Shared { get; set; }
        /// <summary>Asset to use as the cover; fetch it from the account's thumbnail endpoint.</summary>
        public Guid? ThumbnailAssetId { get; set; }
    }

    public class AdminPersonDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AdminTagDto
    {
        public Guid Id { get; set; }
        /// <summary>The full tag path (e.g. "Family/Kids"); this is what the Tags setting stores.</summary>
        public string Value { get; set; } = string.Empty;
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
        [ProducesResponseType(typeof(SettingsUpdateResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SettingsUpdateResultDto>> Setup([FromBody] AdminSetupDto setup)
        {
            if (string.IsNullOrWhiteSpace(setup.AdminPassword))
            {
                return Problem(detail: "An admin password is required.", statusCode: StatusCodes.Status400BadRequest);
            }

            SetupResult result;
            try
            {
                result = await _settingsService.TryClaimSetupAsync(
                    setup.AdminPassword, () => _adminAuthService.SetupRequired);
            }
            catch (SettingsNotValidException ex)
            {
                _logger.LogWarning("Rejected setup: {message}", ex.Message);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }

            if (result == SetupResult.AlreadyClaimed)
            {
                return Problem(detail: "ImmichFrame is already configured.", statusCode: StatusCodes.Status409Conflict);
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
            // Secrets in the body: keep them out of browser and proxy caches.
            Response.Headers.CacheControl = "no-store";
            return _settingsService.GetRawSettings();
        }

        [HttpPut("Settings", Name = "UpdateAdminSettings")]
        [ProducesResponseType(typeof(SettingsUpdateResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Drops all cached Immich data and makes open slideshows discard their queued photos
        /// (they notice within a couple of minutes).
        /// </summary>
        [HttpPost("RefreshPhotos", Name = "RefreshPhotos")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RefreshPhotos()
        {
            await _settingsService.RefreshContentAsync();
            return NoContent();
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

        /// <summary>
        /// Lists the albums visible to a saved account, so the admin UI can offer a picker
        /// instead of asking for IDs. <paramref name="index"/> is the account's position in the saved settings.
        /// </summary>
        [HttpGet("Accounts/{index:int}/Albums", Name = "GetAccountAlbums")]
        [ProducesResponseType(typeof(List<AdminAlbumDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AdminAlbumDto>>> GetAccountAlbums(int index, CancellationToken ct)
        {
            var api = CreateAccountApi(index, out var problem);
            if (api == null) return problem!;

            try
            {
                var albums = await api.GetAllAlbumsAsync(null, null, null, null, null, ct);
                return Ok(albums
                    .Select(a => new AdminAlbumDto
                    {
                        Id = a.Id,
                        Name = a.AlbumName,
                        AssetCount = (int)a.AssetCount,
                        Shared = a.Shared,
                        ThumbnailAssetId = a.AlbumThumbnailAssetId
                    })
                    .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Could not list albums for account {index}: {message}", index, ex.Message);
                return Problem(detail: $"Could not load albums from Immich: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
            }
        }

        /// <summary>
        /// Proxies an asset thumbnail through the admin API. Browsers cannot attach the admin
        /// password to an img tag, and this keeps the Immich API key on the server.
        /// </summary>
        [HttpGet("Accounts/{index:int}/Assets/{assetId:guid}/Thumbnail", Name = "GetAccountAssetThumbnail")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccountAssetThumbnail(int index, Guid assetId, CancellationToken ct)
        {
            var api = CreateAccountApi(index, out var problem);
            if (api == null) return problem!;

            try
            {
                var data = await api.ViewAssetAsync(null, assetId, string.Empty, AssetMediaSize.Thumbnail, null, ct);
                var contentType = data.Headers.TryGetValue("Content-Type", out var values)
                    ? values.FirstOrDefault() ?? "image/jpeg"
                    : "image/jpeg";
                Response.Headers.CacheControl = "private, max-age=3600";
                return File(data.Stream, contentType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Could not fetch thumbnail {assetId} for account {index}: {message}", assetId, index, ex.Message);
                return NotFound();
            }
        }

        /// <summary>Lists the named people visible to a saved account (unnamed faces are not useful to pick).</summary>
        [HttpGet("Accounts/{index:int}/People", Name = "GetAccountPeople")]
        [ProducesResponseType(typeof(List<AdminPersonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AdminPersonDto>>> GetAccountPeople(int index, CancellationToken ct)
        {
            var api = CreateAccountApi(index, out var problem);
            if (api == null) return problem!;

            try
            {
                var people = new List<AdminPersonDto>();
                for (var page = 1; page <= 50; page++) // hard cap so a misbehaving server cannot loop us forever
                {
                    var result = await api.GetAllPeopleAsync(null, null, page, 500, false, ct);
                    people.AddRange(result.People
                        .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                        .Select(p => new AdminPersonDto { Id = p.Id, Name = p.Name }));
                    if (!result.HasNextPage.GetValueOrDefault()) break;
                }

                return Ok(people.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Could not list people for account {index}: {message}", index, ex.Message);
                return Problem(detail: $"Could not load people from Immich: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
            }
        }

        [HttpGet("Accounts/{index:int}/People/{personId:guid}/Thumbnail", Name = "GetAccountPersonThumbnail")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccountPersonThumbnail(int index, Guid personId, CancellationToken ct)
        {
            var api = CreateAccountApi(index, out var problem);
            if (api == null) return problem!;

            try
            {
                var data = await api.GetPersonThumbnailAsync(personId, ct);
                var contentType = data.Headers.TryGetValue("Content-Type", out var values)
                    ? values.FirstOrDefault() ?? "image/jpeg"
                    : "image/jpeg";
                Response.Headers.CacheControl = "private, max-age=3600";
                return File(data.Stream, contentType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Could not fetch thumbnail of person {personId} for account {index}: {message}", personId, index, ex.Message);
                return NotFound();
            }
        }

        [HttpGet("Accounts/{index:int}/Tags", Name = "GetAccountTags")]
        [ProducesResponseType(typeof(List<AdminTagDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<AdminTagDto>>> GetAccountTags(int index, CancellationToken ct)
        {
            var api = CreateAccountApi(index, out var problem);
            if (api == null) return problem!;

            try
            {
                var tags = await api.GetAllTagsAsync(ct);
                return Ok(tags
                    .Select(t => new AdminTagDto { Id = t.Id, Value = t.Value })
                    .OrderBy(t => t.Value, StringComparer.CurrentCultureIgnoreCase)
                    .ToList());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Could not list tags for account {index}: {message}", index, ex.Message);
                return Problem(detail: $"Could not load tags from Immich: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
            }
        }

        private ImmichApi? CreateAccountApi(int index, out ActionResult? problem)
        {
            var accounts = _settingsService.GetRawSettings().AccountsImpl.ToList();
            if (index < 0 || index >= accounts.Count)
            {
                problem = Problem(detail: "No such account. Save your settings first.", statusCode: StatusCodes.Status404NotFound);
                return null;
            }

            var account = accounts[index];
            try
            {
                account.ValidateAndInitialize(); // resolves ApiKeyFile
            }
            catch (Exception ex)
            {
                problem = Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
                return null;
            }

            var httpClient = _httpClientFactory.CreateClient(ImmichApiHttpClientExtensions.ImmichApiAccountClient);
            httpClient.UseApiKey(account.ApiKey);
            problem = null;
            return new ImmichApi(account.ImmichServerUrl, httpClient);
        }
    }
}
