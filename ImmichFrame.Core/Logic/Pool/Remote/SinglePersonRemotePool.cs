using ImmichFrame.Core.Api;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class SinglePersonRemotePool(Guid personId, ILogger<SinglePersonRemotePool> logger, ImmichApi immichApi) : SearchBasedRemotePool<SinglePersonRemotePool>(immichApi, logger)
{
    protected override void ConfigureCountQuery(StatisticsSearchDto dto) => dto.PersonIds = new List<Guid> { personId };
    protected override void ConfigureAssetQuery(MetadataSearchDto dto) => dto.PersonIds = new List<Guid> { personId };
}