using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Logic;

public class LogicPoolAdapter(IAssetPool pool, ImmichApi immichApi, string? webhook) : IImmichFrameLogic
{
    public async Task<AssetResponseDto?> GetNextAsset()
        => (await pool.GetAssets(1)).FirstOrDefault();

    public Task<IEnumerable<AssetResponseDto>> GetAssets()
        => pool.GetAssets(25);

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
        => immichApi.GetAssetInfoAsync(assetId, null);

    public async Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId)
        => await immichApi.GetAllAlbumsAsync(assetId, null);

    public Task<long> GetTotalAssets() => pool.GetAssetCount();

    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, webhook);

    public virtual async Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
    {
        var data = await immichApi.ViewAssetAsync(id, string.Empty, AssetMediaSize.Preview);

        if (data == null)
            throw new AssetNotFoundException($"Asset {id} was not found!");

        var contentType = "";
        if (data.Headers.ContainsKey("Content-Type"))
        {
            contentType = data.Headers["Content-Type"].FirstOrDefault() ?? "";
        }

        var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
        var fileName = $"{id}.{ext}";

        return (fileName, contentType, data.Stream);
    }
}