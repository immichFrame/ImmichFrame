using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private IImmichFrameLogic _logic;

        public AssetController(ILogger<AssetController> logger, IBaseSettings settings)
        {
            _logger = logger;
            _logic = new ImmichFrameLogic(settings);
        }

        [HttpGet(Name = "GetAsset")]
        public async Task<AssetResponseDto> GetAsset()
        {
            return await _logic.GetNextAsset();
        }

        [HttpGet("{id}", Name = "GetImage")]
        [Produces("image/jpeg", "image/webp")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<FileResult> GetImage(Guid id)
        {
            var data = await _logic.GetImage(id);

            var contentType = "";
            if (data.Headers.ContainsKey("Content-Type"))
            {
                contentType = data.Headers["Content-Type"].First().ToString();
            }

            var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
            var fileName = $"{id}.{ext}";

            return File(data.Stream, contentType, fileName); // returns a FileStreamResult
        }
    }
}
