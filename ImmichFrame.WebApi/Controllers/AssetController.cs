using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
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
            return await _logic.GetNextAsset() ?? throw new AssetNotFoundException("No asset was found");
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
