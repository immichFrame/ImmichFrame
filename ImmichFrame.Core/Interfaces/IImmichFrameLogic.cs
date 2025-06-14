using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<IEnumerable<AssetResponseDto>> GetAssets();
        public Task<AssetResponseDto> GetAssetInfoById(Guid assetId);
        public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId);
        public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id);
        public Task<long> GetTotalAssets();
        public Task SendWebhookNotification(IWebhookNotification notification);
    }
    
    public interface IAccountSelectionStrategy
    {
        Task<(IImmichFrameLogic, AssetResponseDto)?> GetNextAsset(IList<IImmichFrameLogic> accounts);
        Task<(IImmichFrameLogic account, IEnumerable<AssetResponseDto>)[]> GetAssets(IList<IImmichFrameLogic> accounts);
    }
}
