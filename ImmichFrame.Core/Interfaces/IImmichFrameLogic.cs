using ImmichFrame.Core.Api;
using OpenWeatherMap;
using OpenWeatherMap.Models;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto> GetNextAsset();
        public Task<FileResponse> GetImage(Guid id);
        public Task AddAssetToAlbum(AssetResponseDto assetToAdd);
        public Task DeleteAndCreateImmichFrameAlbum();
        public Task<IWeather?> GetWeather();
        public Task<IWeather?> GetWeather(double latitude, double longitude, OpenWeatherMapOptions Options);
    }
}
