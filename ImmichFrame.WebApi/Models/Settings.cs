using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models
{
    public class Settings : IBaseSettings
    {
        public string ImmichServerUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool ShowMemories { get; set; } = false;
        public List<Guid> Albums { get; set; } = new List<Guid>();
        public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
        public List<Guid> People { get; set; } = new List<Guid>();
        public int RefreshAlbumPeopleInterval { get; set; } = 12;
        public string ImmichFrameAlbumName { get; set; } = string.Empty;

        public Settings()
        {
            var env = Environment.GetEnvironmentVariables();
            try
            {
                ImmichServerUrl = env["ImmichServerUrl"]?.ToString() ?? string.Empty;
                ApiKey = env["ApiKey"]?.ToString() ?? string.Empty;
                ShowMemories = bool.Parse(env["ShowMemories"]?.ToString() ?? "false");
                Albums = env["Albums"]?.ToString()?.Split(',').Select(x => new Guid(x)).ToList() ?? new();
                ExcludedAlbums = env["ExcludedAlbums"]?.ToString()?.Split(',').Select(x => new Guid(x)).ToList() ?? new();
                People = env["People"]?.ToString()?.Split(',').Select(x => new Guid(x)).ToList() ?? new();
                RefreshAlbumPeopleInterval = Convert.ToInt32(env["RefreshAlbumPeopleInterval"]);
                ImmichFrameAlbumName = env["ImmichFrameAlbumName"]?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
            }
        }
    }
}
