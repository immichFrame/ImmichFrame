using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PersonAssetsPool(ApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var personAssets = new List<AssetResponseDto>();

        foreach (var personId in accountSettings.People)
        {
            int page = 1;
            int batchSize = 1000;
            int total;
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

                total = personInfo.Assets.Total;

                personAssets.AddRange(personInfo.Assets.Items);
                page++;
            } while (total == batchSize);
        }

        return personAssets;
    }
}