using System.Globalization;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
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

        [HttpGet("RandomImage", Name = "GetRandomImage")]
        [Produces("application/json")]
        public async Task<object> GetRandomImage()
        {
            var _settings = new WebClientSettings();
            var randomImage = await _logic.GetNextAsset() ?? throw new AssetNotFoundException("No asset was found");

            //var randomImageUrl = Url.Action("GetRandomImage", null, new { id = randomImage.Id }, Request.Scheme);
            var image = await _logic.GetImage(new Guid(randomImage.Id));
            string randomImageBase64;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await image.fileStream.CopyToAsync(memoryStream);

                byte[] fileBytes = memoryStream.ToArray();

                randomImageBase64 = Convert.ToBase64String(fileBytes);
            }

            randomImage.ThumbhashImage!.Position = 0;
            byte[] byteArray = new byte[randomImage.ThumbhashImage.Length];
            randomImage.ThumbhashImage.Read(byteArray, 0, byteArray.Length);
            string thumbHashBase64 = Convert.ToBase64String(byteArray);

            CultureInfo cultureInfo = new CultureInfo(_settings.Language);
            string photoDate = randomImage.LocalDateTime.ToString(_settings.PhotoDateFormat, cultureInfo) ?? string.Empty;

            var locationParts = _settings.ImageLocationFormat?.Split(',') ?? Array.Empty<string>();
            var city = locationParts.Length >= 1 ? randomImage.ExifInfo.City : string.Empty;
            var state = locationParts.Length >= 2 ? (randomImage.ExifInfo.State?.Split(", ").Last() ?? string.Empty) : string.Empty;
            var country = locationParts.Length >= 3 ? randomImage.ExifInfo.Country : string.Empty;

            string imageLocation = string.Join(", ", new[] { city, state, country }.Where(part => !string.IsNullOrWhiteSpace(part)));

            return Ok(new
            {
                RandomImageBase64 = randomImageBase64,
                ThumbHashImageBase64 = thumbHashBase64,
                PhotoDate = photoDate,
                ImageLocation = imageLocation
            });
        }
    }
}
