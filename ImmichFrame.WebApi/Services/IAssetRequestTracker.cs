using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

public interface IAssetRequestTracker
{
    void RecordAssetRequest(string clientIdentifier, Guid assetId, string endpoint, string? assetType);
    IReadOnlyCollection<AssetRequestRecord> GetAssetRequests(string? clientIdentifier, Guid? assetId, int limit);
}
