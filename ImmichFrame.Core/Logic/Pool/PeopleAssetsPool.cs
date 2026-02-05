using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PersonAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var personAssets = new List<AssetResponseDto>();

        var people = accountSettings.People;
        if (people == null || people.Count == 0)
        {
            return personAssets;
        }

        foreach (var personId in people)
        {
            personAssets.AddRange(await LoadAssetsForPerson(personId, ct));
        }

        var excludedPersonAssets = new List<AssetResponseDto>();

        foreach (var personId in accountSettings.ExcludedPeople)
        {
            excludedPersonAssets.AddRange(await LoadAssetsForPerson(personId, ct));
        }

        return personAssets.WhereExcludes(excludedPersonAssets, t => t.Id);
    }

    private async Task<List<AssetResponseDto>> LoadAssetsForPerson(Guid personId, CancellationToken ct)
    {
        var assets = new List<AssetResponseDto>();
        int page = 1;
        int batchSize = 1000;
        int lastPageCount;

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

            lastPageCount = personInfo.Assets.Items.Count;

            assets.AddRange(personInfo.Assets.Items);
            page++;
        } while (lastPageCount == batchSize);

        return assets;
    }
}