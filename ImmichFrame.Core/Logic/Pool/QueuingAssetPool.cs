using System.Threading.Channels;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool;

public class QueuingAssetPool(ILogger<QueuingAssetPool> _logger, IAssetPool @delegate) : AggregatingAssetPool
{
    private const int RELOAD_BATCH_SIZE = 50;
    private const int RELOAD_THRESHOLD = 10;

    private readonly SemaphoreSlim _isReloadingAssets = new(1, 1);
    private Channel<AssetResponseDto> _assetQueue = Channel.CreateUnbounded<AssetResponseDto>();

    public override Task<long> GetAssetCount(CancellationToken ct = default) => @delegate.GetAssetCount(ct);

    protected override async Task<AssetResponseDto?> GetNextAsset(IRequestContext requestContext, CancellationToken ct)
    {
        try
        {
            if (_assetQueue.Reader.Count <= RELOAD_THRESHOLD)
            {
                // Fire-and-forget, reloading assets in the background
                _ = ReloadAssetsAsync(requestContext);
            }

            return await _assetQueue.Reader.ReadAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // This exception occurs if the CancellationTokenSource times out
            _logger.LogWarning("Read asset list timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An unexpected error occurred while reading assets: {ex.Message}");
            throw;
        }
    }

    private async Task ReloadAssetsAsync(IRequestContext requestContext)
    {
        if (await _isReloadingAssets.WaitAsync(0))
        {
            try
            {
                _logger.LogDebug("Reloading assets");

                // TODO: apply account filters - QueuingAssetPool is currently not used anywhere
                foreach (var asset in await @delegate.GetAssets(RELOAD_BATCH_SIZE, requestContext))
                {
                    await _assetQueue.Writer.WriteAsync(asset);
                }
            }
            finally
            {
                _isReloadingAssets.Release();
            }
        }
        else
        {
            _logger.LogDebug("Assets already being loaded; not attempting a concurrent reload");
        }
    }
}
