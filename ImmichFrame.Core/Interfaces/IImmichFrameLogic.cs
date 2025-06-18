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
        void Initialize(IList<IImmichFrameLogic> accounts);
        Task<AssetResponseDto?> GetNextAsset();
        Task<IEnumerable<AssetResponseDto>> GetAssets();
        T ForAsset<T>(Guid assetId, Func<IImmichFrameLogic, T> f);
    }
}
