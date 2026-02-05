using System.Globalization;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    public class ImageResponse
    {
        public required string RandomImageBase64 { get; set; }
        public required string ThumbHashImageBase64 { get; set; }
        public required string PhotoDate { get; set; }
        public required string ImageLocation { get; set; }
    }


    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IImmichFrameLogic _logic;
        private readonly IGeneralSettings _settings;

        private readonly IRequestContext _requestContext;

        public AssetController(ILogger<AssetController> logger, IImmichFrameLogic logic, IGeneralSettings settings, IRequestContext requestContext)
        {
            _logger = logger;
            _logic = logic;
            _settings = settings;
            _requestContext = requestContext;
        }

        [HttpGet(Name = "GetAssets")]
        public async Task<AssetListResponseDto> GetAssets(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Assets requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);

            AssetListResponseDto response = new AssetListResponseDto();

            response.Assets = (await _logic.GetAssets(_requestContext)).ToList();
            response.AssetOffset = _requestContext.AssetOffset;

            return response;
        }

        [HttpGet("{id}/AssetInfo", Name = "GetAssetInfo")]
        public async Task<AssetResponseDto> GetAssetInfo(Guid id, string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("AssetInfo '{id}' requested by '{sanitizedClientIdentifier}'", id, sanitizedClientIdentifier);

            return await _logic.GetAssetInfoById(id);
        }

        [HttpGet("{id}/AlbumInfo", Name = "GetAlbumInfo")]
        public async Task<List<AlbumResponseDto>> GetAlbumInfo(Guid id, string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("AlbumInfo '{id}' requested by '{sanitizedClientIdentifier}'", id, sanitizedClientIdentifier);

            return (await _logic.GetAlbumInfoById(id)).ToList() ?? throw new AssetNotFoundException("No asset was found");
        }

        [Obsolete("Use GetAsset instead.")]
        [HttpGet("{id}/Image", Name = "GetImage")]
        [Produces("image/jpeg", "image/webp")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetImage(Guid id, string clientIdentifier = "")
        {
            return await GetAsset(id, clientIdentifier, AssetTypeEnum.IMAGE);
        }

        [HttpGet("{id}/Asset", Name = "GetAsset")]
        [Produces("image/jpeg", "image/webp", "video/mp4", "video/quicktime")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetAsset(Guid id, string clientIdentifier = "", AssetTypeEnum? assetType = null)
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Asset '{id}' requested by '{sanitizedClientIdentifier}' (type hint: {assetType})", id, sanitizedClientIdentifier, assetType);
            var asset = await _logic.GetAsset(id, assetType);

            var notification = new AssetRequestedNotification(id, sanitizedClientIdentifier);
            _ = _logic.SendWebhookNotification(notification);

            return File(asset.fileStream, asset.ContentType, asset.fileName, enableRangeProcessing: true);
        }

        [HttpGet("RandomImageAndInfo", Name = "GetRandomImageAndInfo")]
        [Produces("application/json")]
        public async Task<ImageResponse> GetRandomImageAndInfo(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Random image requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);

            AssetResponseDto? randomAsset = null;
            const int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                var candidate = await _logic.GetNextAsset(_requestContext);
                if (candidate == null) break;
                if (candidate.Type == AssetTypeEnum.IMAGE)
                {
                    randomAsset = candidate;
                    break;
                }
            }

            if (randomAsset == null)
                throw new AssetNotFoundException("No image asset was found");

            var asset = await _logic.GetAsset(new Guid(randomAsset.Id), AssetTypeEnum.IMAGE);
            var notification = new AssetRequestedNotification(new Guid(randomAsset.Id), sanitizedClientIdentifier);
            _ = _logic.SendWebhookNotification(notification);

            string randomImageBase64;
            using (var memoryStream = new MemoryStream())
            {
                await asset.fileStream.CopyToAsync(memoryStream);
                randomImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            randomAsset.ThumbhashImage!.Position = 0;
            byte[] byteArray = new byte[randomAsset.ThumbhashImage.Length];
            randomAsset.ThumbhashImage.Read(byteArray, 0, byteArray.Length);
            string thumbHashBase64 = Convert.ToBase64String(byteArray);

            CultureInfo cultureInfo = new CultureInfo(_settings.Language);
            string photoDateFormat = _settings.PhotoDateFormat!.Replace("''", "\\'");
            string photoDate = randomAsset.LocalDateTime.ToString(photoDateFormat, cultureInfo) ?? string.Empty;

            var locationFormat = _settings.ImageLocationFormat ?? "City,State,Country";
            var imageLocation = locationFormat
                .Replace("City", randomAsset.ExifInfo?.City ?? string.Empty)
                .Replace("State", randomAsset.ExifInfo?.State ?? string.Empty)
                .Replace("Country", randomAsset.ExifInfo?.Country ?? string.Empty);
            imageLocation = string.Join(",", imageLocation.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)));

            return new ImageResponse
            {
                RandomImageBase64 = randomImageBase64,
                ThumbHashImageBase64 = thumbHashBase64,
                PhotoDate = photoDate,
                ImageLocation = imageLocation
            };
        }
    }
}
