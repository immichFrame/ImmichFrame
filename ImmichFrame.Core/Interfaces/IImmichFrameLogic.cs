using ImmichFrame.Core.Api;


namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset(IRequestContext requestContext);
        public Task<IEnumerable<AssetResponseDto>> GetAssets(IRequestContext requestContext);
        public Task<AssetResponseDto> GetAssetInfoById(Guid assetId);
        public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId);
        public Task<(string fileName, string ContentType, Stream fileStream)> GetAsset(Guid id, AssetTypeEnum? assetType = null);
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
        Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset(IRequestContext requestContext);
        Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets(IRequestContext requestContext);
        T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f);
    }
}
