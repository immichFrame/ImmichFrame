using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using ImmichFrame.Core.Logic.Pool.Preload;
using ImmichFrame.Core.Logic.Pool.Remote;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic;

public interface IAssetPoolFactory
{
    IAssetPool BuildPool(IAccountSettings accountSettings, IApiCache apiCache, ImmichApi immichApi);
}

public class AssetPoolFactory(ILoggerFactory loggerFactory) : IAssetPoolFactory
{
    public IAssetPool BuildPool(IAccountSettings accountSettings, IApiCache apiCache, ImmichApi immichApi)
    {
        if (!accountSettings.ShowFavorites && !accountSettings.ShowMemories && !accountSettings.Albums.Any() && !accountSettings.People.Any())
        {
            return new AllAssetsRemotePool(apiCache, immichApi, accountSettings, loggerFactory.CreateLogger<AllAssetsRemotePool>());
        }

        var pools = new List<IAssetPool>();

        if (accountSettings.Albums.Any() || accountSettings.People.Any())
            pools.Add(
                new CircuitBreakerPool(
                    new FilteredAssetsRemotePool(apiCache, immichApi, accountSettings, loggerFactory.CreateLogger<FilteredAssetsRemotePool>()),
                    new MultiAssetPool(new List<IAssetPool>
                    {
                        new AlbumAssetsPreloadPool(apiCache, immichApi, accountSettings),
                        new PersonAssetsPreloadPool(apiCache, immichApi, accountSettings)
                    }),
                    loggerFactory.CreateLogger<CircuitBreakerPool>()
                ));

        if (accountSettings.ShowFavorites)
            pools.Add(
                new CircuitBreakerPool(
                    new FavoriteAssetsRemotePool(loggerFactory.CreateLogger<FavoriteAssetsRemotePool>(), immichApi),
                    new FavoriteAssetsPreloadPool(apiCache, immichApi, accountSettings),
                    loggerFactory.CreateLogger<CircuitBreakerPool>()
                ));

        if (accountSettings.ShowMemories)
            pools.Add(new MemoryAssetsPreloadPool(apiCache, immichApi, accountSettings));


        return new MultiAssetPool(pools);
    }
}