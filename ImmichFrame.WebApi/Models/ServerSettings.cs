using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

namespace ImmichFrame.WebApi.Models
{
    public class ServerSettings : IServerSettings, IConfigSettable
    {
        public string ImmichServerUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool ShowMemories { get; set; } = false;
        public bool ShowFavorites { get; set; } = false;
        public bool ShowArchived { get; set; } = false;
        public bool DownloadImages { get; set; } = false;
        public int RenewImagesDuration { get; set; } = 30;
        public int? ImagesFromDays { get; set; }
        public DateTime? ImagesFromDate { get; set; }
        public DateTime? ImagesUntilDate { get; set; }
        public List<Guid> Albums { get; set; } = new List<Guid>();
        public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
        public List<Guid> People { get; set; } = new List<Guid>();
        public int? Rating { get; set; }
        public List<string> Webcalendars { get; set; } = new List<string>();
        public int RefreshAlbumPeopleInterval { get; set; } = 12;
        public string ImmichFrameAlbumName { get; set; } = string.Empty;
        public string? WeatherApiKey { get; set; } = string.Empty;
        public string? UnitSystem { get; set; } = "imperial";
        public string? WeatherLatLong { get; set; } = "40.7128,74.0060";
        public string Language { get; set; } = "en";
        public string? Webhook { get; set; }
        public string? AuthenticationSecret { get; set; }
    }
}
