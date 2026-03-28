using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PersonAssetsPool : CachingApiAssetsPool
{
    public PersonAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
        : base(apiCache, immichApi, accountSettings)
    {
    }

    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var personAssets = new List<AssetResponseDto>();

        var people = AccountSettings.People;
        if (people == null)
        {
            return personAssets;
        }
        
        foreach (var personId in people)
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
                    WithExif = true,
                    WithPeople = true
                };

                if (!AccountSettings.ShowVideos)
                {
                    metadataBody.Type = AssetTypeEnum.IMAGE;
                }

                var personInfo = await ImmichApi.SearchAssetsAsync(metadataBody, ct);

                total = personInfo.Assets.Total;

                personAssets.AddRange(personInfo.Assets.Items);
                page++;
            } while (total == batchSize);
        }

        return personAssets;
    }
}
