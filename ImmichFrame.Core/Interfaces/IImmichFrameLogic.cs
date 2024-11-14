using ImmichFrame.Core.Api;
using OpenWeatherMap;

namespace ImmichFrame.Core.Interfaces
{
    public interface IImmichFrameLogic
    {
        public Task<AssetResponseDto?> GetNextAsset();
        public Task<List<AssetResponseDto>> GetAssets();
        public Task<FileResponse> GetImage(Guid id);
        public Task AddAssetToAlbum(AssetResponseDto assetToAdd);
        public Task DeleteAndCreateImmichFrameAlbum();
        public Task<List<IAppointment>> GetAppointments();
        public Task<IWeather?> GetWeather();
        public Task<IWeather?> GetWeather(double latitude, double longitude, OpenWeatherMapOptions Options);
    }
}
