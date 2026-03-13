using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetRequestsController(IAssetRequestTracker tracker, IImmichFrameLogic logic, ILogger<AssetRequestsController> logger) : ControllerBase
{
    [HttpGet("AssetRequests")]
    [Produces("application/json")]
    public ActionResult<IReadOnlyCollection<AssetRequestRecord>> GetAssetRequests(string? clientIdentifier = null, Guid? assetId = null, int limit = 100)
    {
        var sanitizedClientIdentifier = clientIdentifier?.SanitizeString() ?? string.Empty;
        return Ok(tracker.GetAssetRequests(sanitizedClientIdentifier, assetId, limit));
    }

    [HttpGet("RecentAssetRequests")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyCollection<RecentAssetRequestDetailsRecord>>> GetRecentAssetRequests(string? clientIdentifier = null, int limit = 10)
    {
        var sanitizedClientIdentifier = clientIdentifier?.SanitizeString() ?? string.Empty;
        var requests = tracker.GetAssetRequests(sanitizedClientIdentifier, null, 1000);
        var uniqueRecentRequests = requests
            .GroupBy(request => request.AssetId)
            .Select(group => group.First())
            .Take(Math.Clamp(limit, 1, 1000));
        var enrichedRequests = await Task.WhenAll(uniqueRecentRequests.Select(EnrichAssetRequestAsync));
        return Ok(enrichedRequests);
    }

    private async Task<RecentAssetRequestDetailsRecord> EnrichAssetRequestAsync(AssetRequestRecord request)
    {
        try
        {
            var assetInfo = await logic.GetAssetInfoById(request.AssetId);
            var city = assetInfo.ExifInfo?.City;
            var state = assetInfo.ExifInfo?.State;
            var country = assetInfo.ExifInfo?.Country;

            return new RecentAssetRequestDetailsRecord
            {
                AssetId = request.AssetId,
                ClientIdentifier = request.ClientIdentifier,
                RequestedAtUtc = request.RequestedAtUtc,
                Endpoint = request.Endpoint,
                AssetType = request.AssetType,
                OriginalFileName = assetInfo.OriginalFileName,
                TakenAtUtc = assetInfo.LocalDateTime,
                Location = string.Join(", ", new[] { city, state, country }.Where(part => !string.IsNullOrWhiteSpace(part))),
                City = city,
                State = state,
                Country = country
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enrich recent asset request for asset '{assetId}'", request.AssetId);

            return new RecentAssetRequestDetailsRecord
            {
                AssetId = request.AssetId,
                ClientIdentifier = request.ClientIdentifier,
                RequestedAtUtc = request.RequestedAtUtc,
                Endpoint = request.Endpoint,
                AssetType = request.AssetType,
                OriginalFileName = null,
                TakenAtUtc = null,
                Location = null,
                City = null,
                State = null,
                Country = null
            };
        }
    }
}
