using ImmichFrame.Core.Api;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class FavoriteAssetsRemotePool(ILogger<FavoriteAssetsRemotePool> _logger, ImmichApi _immichApi) : IAssetPool
{
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        try
        {
            return (await _immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto
            {
                IsFavorite = true,
            }, ct)).Total;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to get asset count, falling back to preload  [{e.Message}]");
            throw;
        }
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        try
        {
            return (await _immichApi.SearchAssetsAsync(new MetadataSearchDto
            {
                Size = requested,
                IsFavorite = true,
            }, ct)).Assets.Items;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to get assets, falling back to preload [{e.Message}]");
            throw;
        }
    }
}