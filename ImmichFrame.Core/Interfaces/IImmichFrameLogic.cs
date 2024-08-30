using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto> GetNextAsset();
        public Task<FileResponse> GetImage(Guid id);
        public Task AddAssetToAlbum(AssetResponseDto assetToAdd);
        public Task DeleteAndCreateImmichFrameAlbum();
    }
}
