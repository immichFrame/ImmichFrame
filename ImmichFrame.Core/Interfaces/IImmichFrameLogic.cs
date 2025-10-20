﻿using ImmichFrame.Core.Api;


namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<IEnumerable<AssetResponseDto>> GetAssets();
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
        Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset();
        Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets();
        T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f);
    }
}
