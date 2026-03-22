using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PersonAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var personAssets = new List<AssetResponseDto>();

        var people = accountSettings.People;
        if (people == null)
        {
            return personAssets;
        }

        // AND mode: pass all person IDs in a single query so the API returns only
        // assets that feature every person in the list.
        // OR mode (default): query each person separately and combine results.
        var personIdGroups = accountSettings.RequireAllPeople
            ? [people]
            : people.Select(id => (IList<Guid>)[id]);

        foreach (var personIds in personIdGroups)
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
                    PersonIds = personIds,
                    WithExif = true,
                    WithPeople = true
                };

                if (!accountSettings.ShowVideos)
                {
                    metadataBody.Type = AssetTypeEnum.IMAGE;
                }

                var personInfo = await immichApi.SearchAssetsAsync(metadataBody, ct);

                total = personInfo.Assets.Total;

                personAssets.AddRange(personInfo.Assets.Items);
                page++;
            } while (total == batchSize);
        }

        return personAssets;
    }
}