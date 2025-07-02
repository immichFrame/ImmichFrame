using ImmichFrame.Core.Api;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class FavoriteAssetsRemotePool(ILogger<FavoriteAssetsRemotePool> logger, ImmichApi immichApi) : SearchBasedRemotePool<FavoriteAssetsRemotePool>(immichApi, logger)
{
    protected override void ConfigureAssetQuery(MetadataSearchDto dto) => dto.IsFavorite = true;
    protected override void ConfigureCountQuery(StatisticsSearchDto dto) => dto.IsFavorite = true;
}