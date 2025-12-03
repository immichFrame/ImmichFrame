using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PersonAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, accountSettings)
{
    protected override async Task<IList<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var personAssets = new List<AssetResponseDto>();

        foreach (var personId in accountSettings.People)
        {
            int page = 1;
            int batchSize = 1000;
            int returned;
            do
            {
                var metadataBody = new MetadataSearchDto
                {
                    Page = page,
                    Size = batchSize,
                    PersonIds = [personId],
                    Type = AssetTypeEnum.IMAGE,
                    WithExif = true,
                    WithPeople = true
                };

                var personInfo = await immichApi.SearchAssetsAsync(metadataBody, ct);
                
                var items = personInfo.Assets.Items;
                returned = items.Count;
                personAssets.AddRange(items);
                page++;
            } while (returned == batchSize && !ct.IsCancellationRequested);
        }

        return personAssets;
    }
}