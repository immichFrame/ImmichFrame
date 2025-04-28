using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<IEnumerable<AssetResponseDto>> GetAssets();
        public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id);
        // public Task AddAssetToAlbum(AssetResponseDto assetToAdd);
        // public Task DeleteAndCreateImmichFrameAlbum();
        public Task SendWebhookNotification(IWebhookNotification notification);
    }
}
