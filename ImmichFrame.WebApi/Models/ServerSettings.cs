using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

namespace ImmichFrame.WebApi.Models
{
    public class ServerSettings : IServerSettings
    {
        public string ImmichServerUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool ShowMemories { get; set; } = false;
        public List<Guid> Albums { get; set; } = new List<Guid>();
        public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
        public List<Guid> People { get; set; } = new List<Guid>();
        public int RefreshAlbumPeopleInterval { get; set; } = 12;
        public string ImmichFrameAlbumName { get; set; } = string.Empty;

        public ServerSettings()
        {
            var env = Environment.GetEnvironmentVariables();
            try
            {
                foreach (var key in env.Keys)
                {
                    if (key == null) continue;

                    var propertyInfo = typeof(ServerSettings).GetProperty(key.ToString());

                    if (propertyInfo != null)
                    {
                        this.SetValue(propertyInfo, env[key].ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
            }
        }
    }
}
