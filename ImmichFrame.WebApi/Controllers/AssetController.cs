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

        public AssetController(ILogger<AssetController> logger, IImmichFrameLogic logic, IGeneralSettings settings)
        {
            _logger = logger;
            _logic = logic;
            _settings = settings;
        }

        [HttpGet(Name = "GetAsset")]
        public async Task<List<AssetResponseDto>> GetAsset(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Assets requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return (await _logic.GetAssets()).ToList() ?? throw new AssetNotFoundException("No asset was found");
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

        [HttpGet("{id}/Image", Name = "GetImage")]
        [Produces("image/jpeg", "image/webp")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetImage(Guid id, string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Image '{id}' requested by '{sanitizedClientIdentifier}'", id, sanitizedClientIdentifier);
            var image = await _logic.GetImage(id);

            var notification = new ImageRequestedNotification(id, sanitizedClientIdentifier);
            _ = _logic.SendWebhookNotification(notification);

            return File(image.fileStream, image.ContentType, image.fileName); // returns a FileStreamResult
        }

        [HttpGet("RandomImageAndInfo", Name = "GetRandomImageAndInfo")]
        [Produces("application/json")]
        public async Task<ImageResponse> GetRandomImageAndInfo(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Random image requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);

            var randomImage = await _logic.GetNextAsset() ?? throw new AssetNotFoundException("No asset was found");

            var image = await _logic.GetImage(new Guid(randomImage.Id));
            var notification = new ImageRequestedNotification(new Guid(randomImage.Id), sanitizedClientIdentifier);
            _ = _logic.SendWebhookNotification(notification);

            string randomImageBase64;
            using (var memoryStream = new MemoryStream())
            {
                await image.fileStream.CopyToAsync(memoryStream);
                randomImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            randomImage.ThumbhashImage!.Position = 0;
            byte[] byteArray = new byte[randomImage.ThumbhashImage.Length];
            randomImage.ThumbhashImage.Read(byteArray, 0, byteArray.Length);
            string thumbHashBase64 = Convert.ToBase64String(byteArray);

            CultureInfo cultureInfo = new CultureInfo(_settings.Language);
            string photoDateFormat = _settings.PhotoDateFormat!.Replace("''", "\\'");
            string photoDate = randomImage.LocalDateTime.ToString(photoDateFormat, cultureInfo) ?? string.Empty;

            var locationFormat = _settings.ImageLocationFormat ?? "City,State,Country";
            var imageLocation = locationFormat
                .Replace("City", randomImage.ExifInfo?.City ?? string.Empty)
                .Replace("State", randomImage.ExifInfo?.State ?? string.Empty)
                .Replace("Country", randomImage.ExifInfo?.Country ?? string.Empty);
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
