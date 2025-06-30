using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool.Preload;

public class PersonAssetsPreloadPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : PreloadedAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var all = accountSettings.People.Select(async personId =>
        
            await LoadAssetsFromMetadataSearch(new MetadataSearchDto
            {
                PersonIds = [personId],
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                WithPeople = true
            }, ct)

        );

        return (await Task.WhenAll(all)).SelectMany(x => x);
    }
}