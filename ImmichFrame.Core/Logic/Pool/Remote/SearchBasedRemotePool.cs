using ImmichFrame.Core.Api;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public abstract class SearchBasedRemotePool<T>(ImmichApi immichApi, ILogger<T> logger) : IAssetPool where T : SearchBasedRemotePool<T>
{
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        try
        {
            var query = new StatisticsSearchDto
            {
            };

            ConfigureCountQuery(query);

            return (await immichApi.SearchAssetStatisticsAsync(query, ct)).Total;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to get asset count, falling back to preload  [{e.Message}]");
            throw;
        }
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        try
        {
            var query = new MetadataSearchDto
            {
                Size = requested
            };

            ConfigureAssetQuery(query);

            return (await immichApi.SearchAssetsAsync(query, ct)).Assets.Items;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to get assets, falling back to preload [{e.Message}]");
            throw;
        }
    }

    protected abstract void ConfigureCountQuery(StatisticsSearchDto dto);
    protected abstract void ConfigureAssetQuery(MetadataSearchDto dto);
}