using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Logic;

public class PooledImmichFrameLogic : LogicPoolAdapter, IAccountImmichFrameLogic
{
    private readonly ImmichApi _immichApi;
    public IAccountSettings AccountSettings { get; }

    public PooledImmichFrameLogic(IGeneralSettings generalSettings, IAccountSettings accountSettings, PoolConfiguration poolConfiguration, IHttpClientFactory httpClientFactory)
        : base(BuildPool(accountSettings, poolConfiguration), poolConfiguration.ImmichApi, generalSettings.Webhook)
    {
        _immichApi = poolConfiguration.ImmichApi;
        AccountSettings = accountSettings;
    }

    private static IAssetPool BuildPool(IAccountSettings accountSettings, PoolConfiguration poolConfiguration)
    {
        if (!accountSettings.ShowFavorites && !accountSettings.ShowMemories && !accountSettings.Albums.Any() && !accountSettings.People.Any())
        {
            return new AllAssetsPool(poolConfiguration.ApiCache, poolConfiguration.ImmichApi, accountSettings);
        }

        var pools = new List<IAssetPool>();

        if (accountSettings.ShowFavorites)
            pools.Add(new FavoriteAssetsPool(poolConfiguration.ApiCache, poolConfiguration.ImmichApi, accountSettings));

        if (accountSettings.ShowMemories)
            pools.Add(new MemoryAssetsPool(poolConfiguration.ImmichApi, accountSettings));

        if (accountSettings.Albums.Any())
            pools.Add(new AlbumAssetsPool(poolConfiguration.ApiCache, poolConfiguration.ImmichApi, accountSettings));

        if (accountSettings.People.Any())
            pools.Add(new PersonAssetsPool(poolConfiguration.ApiCache, poolConfiguration.ImmichApi, accountSettings));

        return new MultiAssetPool(pools);
    }

    public override string ToString() => $"Account Pool [{_immichApi.BaseUrl}]";
}