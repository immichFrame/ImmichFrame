using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class SingleAlbumRemotePool(Guid albumId, ILogger<SingleAlbumRemotePool> logger, ImmichApi immichApi) : SearchBasedRemotePool<SingleAlbumRemotePool>(immichApi, logger)
{
    protected override void ConfigureCountQuery(StatisticsSearchDto dto) => dto.AlbumIds = new List<Guid> { albumId };
    protected override void ConfigureAssetQuery(MetadataSearchDto dto) => dto.AlbumIds = new List<Guid> { albumId };
}