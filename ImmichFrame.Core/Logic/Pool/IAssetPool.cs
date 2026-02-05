using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public interface IAssetPool
{
    Task<long> GetAssetCount(CancellationToken ct = default);
    Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, IRequestContext requestContext, CancellationToken ct = default);

    protected static async Task<IEnumerable<AssetResponseDto>> WaitAssets(
        int requested,
        Func<IRequestContext, CancellationToken, Task<AssetResponseDto?>> supplier,
        IRequestContext requestContext,
        CancellationToken? cancellationToken = null)
    {
        //allow up to one minute
        var ct = cancellationToken ?? new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

        var itemsRead = new List<AssetResponseDto>(requested > 0 ? requested : 0);

        for (var i = 0; i < requested; i++)
        {
            var asset = await supplier(requestContext, ct);

            if (asset != null)
            {
                itemsRead.Add(asset);
            }
            else
            {
                break;
            }
        }

        return itemsRead;
    }
}
