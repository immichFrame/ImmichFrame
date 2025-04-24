using ImmichFrame.Core.Api;
using OpenWeatherMap;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<List<AssetResponseDto>> GetAssets();
        public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id);
        // public Task AddAssetToAlbum(AssetResponseDto assetToAdd);
        // public Task DeleteAndCreateImmichFrameAlbum();
        public Task SendWebhookNotification(IWebhookNotification notification);
    }
}
