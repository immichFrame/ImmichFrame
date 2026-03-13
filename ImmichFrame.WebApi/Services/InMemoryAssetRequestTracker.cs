using System.Collections.Concurrent;
using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

internal sealed class InMemoryAssetRequestTracker : IAssetRequestTracker
{
    private const string AnonymousClientIdentifier = "anonymous";
    private const int MaxAssetHistory = 1000;

    private readonly ConcurrentQueue<AssetRequestRecord> _assetRequests = new();

    public void RecordAssetRequest(string clientIdentifier, Guid assetId, string endpoint, string? assetType)
    {
        _assetRequests.Enqueue(new AssetRequestRecord
        {
            AssetId = assetId,
            ClientIdentifier = NormalizeClientIdentifier(clientIdentifier),
            RequestedAtUtc = DateTimeOffset.UtcNow,
            Endpoint = endpoint,
            AssetType = assetType
        });

        while (_assetRequests.Count > MaxAssetHistory && _assetRequests.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyCollection<AssetRequestRecord> GetAssetRequests(string? clientIdentifier, Guid? assetId, int limit)
    {
        var normalizedClientIdentifier = string.IsNullOrWhiteSpace(clientIdentifier)
            ? null
            : NormalizeClientIdentifier(clientIdentifier);

        return _assetRequests
            .Reverse()
            .Where(record => normalizedClientIdentifier == null || string.Equals(record.ClientIdentifier, normalizedClientIdentifier, StringComparison.OrdinalIgnoreCase))
            .Where(record => assetId == null || record.AssetId == assetId.Value)
            .Take(Math.Clamp(limit, 1, MaxAssetHistory))
            .ToArray();
    }

    private static string NormalizeClientIdentifier(string clientIdentifier) =>
        string.IsNullOrWhiteSpace(clientIdentifier) ? AnonymousClientIdentifier : clientIdentifier;
}
