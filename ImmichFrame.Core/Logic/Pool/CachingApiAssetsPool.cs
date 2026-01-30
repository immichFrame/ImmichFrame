using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class CachingApiAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    private readonly Random _random = new();

    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return (await AllAssets(ct)).Count();
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        return (await AllAssets(ct)).OrderBy(_ => _random.Next()).Take(requested);
    }

    private async Task<IEnumerable<AssetResponseDto>> AllAssets(CancellationToken ct = default)
    {
        var excludedAlbumAssets = await apiCache.GetOrAddAsync($"{GetType().FullName}_ExcludedAlbums", () => GetExcludedAlbumAssets(ct));

        return await apiCache.GetOrAddAsync(GetType().FullName!, () => LoadAssets(ct).ApplyAccountFilters(accountSettings, excludedAlbumAssets));
    }

    private async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets(CancellationToken ct = default)
    {
        var excludedAlbumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings?.ExcludedAlbums ?? new())
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);

            if (albumInfo.Assets != null)
            {
                excludedAlbumAssets.AddRange(albumInfo.Assets);
            }
        }

        return excludedAlbumAssets;
    }

    protected abstract Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default);
}