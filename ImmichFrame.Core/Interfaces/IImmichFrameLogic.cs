using ImmichFrame.Core.Api;
using ImmichFrame.Core.Models;


namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<IEnumerable<AssetResponseDto>> GetAssets();
        public Task<AssetResponseDto> GetAssetInfoById(Guid assetId);
        public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId);
        public Task<AssetResponse> GetAsset(Guid id, AssetTypeEnum? assetType = null, string? rangeHeader = null);
        public Task<long> GetTotalAssets();
        public Task SendWebhookNotification(IWebhookNotification notification);
    }

    public interface IAccountImmichFrameLogic : IImmichFrameLogic
    {
        public IAccountSettings AccountSettings { get; }

    }

    public interface IAccountSelectionStrategy
    {
        void Initialize(IList<IAccountImmichFrameLogic> accounts);
        Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset();
        Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets();
        T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f);
    }
}
