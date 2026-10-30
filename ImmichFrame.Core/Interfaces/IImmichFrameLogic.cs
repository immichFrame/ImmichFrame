using ImmichFrame.Core.Api;
using ImmichFrame.Core.Models;


namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset(string clientId = "", CancellationToken ct = default);
        public Task<IEnumerable<AssetResponseDto>> GetAssets(string clientId = "", CancellationToken ct = default);
        public Task<AssetResponseDto> GetAssetInfoById(Guid assetId);
        public Task<IEnumerable<AssetFaceResponseDto>> GetAssetFacesById(Guid assetId);
        public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId);
        public Task<AssetResponse> GetAsset(Guid id, AssetTypeEnum? assetType = null, string? rangeHeader = null);
        public Task<long> GetTotalAssets();
        public Task SendWebhookNotification(IWebhookNotification notification);
    }

    public interface IAccountImmichFrameLogic : IImmichFrameLogic
    {
        public IAccountSettings AccountSettings { get; }
        public Task<IEnumerable<AssetResponseDto>> GetAssets(int count, string clientId, CancellationToken ct = default);
    }

    public interface IAccountSelectionStrategy
    {
        Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset(string clientId = "", CancellationToken ct = default);
        Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets(string clientId = "", CancellationToken ct = default);
        T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f);
    }
}
