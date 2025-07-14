using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Logic;

public class PoolConfiguration
{
    public ImmichApi ImmichApi;
    public IAssetPool AssetPool;
    public ApiCache ApiCache;
    
    public PoolConfiguration(IAccountSettings accountSettings, IGeneralSettings generalSettings)
    {
        var httpClient = new HttpClient();
        httpClient.UseApiKey(accountSettings.ApiKey);
        
        ImmichApi = new ImmichApi(accountSettings.ImmichServerUrl, httpClient);
        ApiCache = new ApiCache(TimeSpan.FromHours(generalSettings.RefreshAlbumPeopleInterval));
        AssetPool = BuildPool(accountSettings, ApiCache, ImmichApi);
    }
    
    private static IAssetPool BuildPool(IAccountSettings accountSettings, ApiCache apiCache, ImmichApi immichApi)
    {
        if (!accountSettings.ShowFavorites && !accountSettings.ShowMemories && !accountSettings.Albums.Any() && !accountSettings.People.Any())
        {
            return new AllAssetsPool(apiCache, immichApi, accountSettings);
        }

        var pools = new List<IAssetPool>();

        if (accountSettings.ShowFavorites)
            pools.Add(new FavoriteAssetsPool(apiCache, immichApi, accountSettings));

        if (accountSettings.ShowMemories)
            pools.Add(new MemoryAssetsPool(apiCache, immichApi, accountSettings));

        if (accountSettings.Albums.Any())
            pools.Add(new AlbumAssetsPool(apiCache, immichApi, accountSettings));

        if (accountSettings.People.Any())
            pools.Add(new PersonAssetsPool(apiCache, immichApi, accountSettings));

        return new MultiAssetPool(pools);
    }
}