using System.Globalization;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.WebApi.Models;
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
    public class AssetController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IImmichFrameLogic _logic;

        public AssetController(ILogger<AssetController> logger, ImmichFrameLogic logic)
        {
            _logger = logger;
            _logic = logic;
        }

        [HttpGet(Name = "GetAsset")]
        public async Task<List<AssetResponseDto>> GetAsset()
        {
            return await _logic.GetAssets() ?? throw new AssetNotFoundException("No asset was found");
        }

        [HttpGet("{id}", Name = "GetImage")]
        [Produces("image/jpeg", "image/webp")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetImage(Guid id)
        {
            var image = await _logic.GetImage(id);

            var notification = new ImageRequestedNotification(id);
            _ = _logic.SendWebhookNotification(notification);

            return File(image.fileStream, image.ContentType, image.fileName); // returns a FileStreamResult
        }

        [HttpGet("RandomImageAndInfo", Name = "GetRandomImageAndInfo")]
        [Produces("application/json")]
        public async Task<ImageResponse> GetRandomImageAndInfo()
        {
            var _settings = new WebClientSettings();
            var randomImage = await _logic.GetNextAsset() ?? throw new AssetNotFoundException("No asset was found");

            var image = await _logic.GetImage(new Guid(randomImage.Id));
            var notification = new ImageRequestedNotification(new Guid(randomImage.Id));
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
            string photoDate = randomImage.LocalDateTime.ToString(_settings.PhotoDateFormat, cultureInfo) ?? string.Empty;

            var locationFormat = _settings.ImageLocationFormat ?? "City,State,Country";
            var imageLocation = locationFormat
                .Replace("City", randomImage.ExifInfo.City ?? string.Empty)
                .Replace("State", randomImage.ExifInfo.State?.Split(", ").Last() ?? string.Empty)
                .Replace("Country", randomImage.ExifInfo.Country ?? string.Empty);

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
