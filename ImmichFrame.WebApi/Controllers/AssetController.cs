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

        [HttpGet("random", Name = "GetRandom")]
        [Produces("image/jpeg", "image/webp")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetNextAsset()
        {
            var randomImage = await _logic.GetNextAsset() ?? throw new AssetNotFoundException("No asset was found");
            return await GetImage(new Guid(randomImage.Id));
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
    }
}
