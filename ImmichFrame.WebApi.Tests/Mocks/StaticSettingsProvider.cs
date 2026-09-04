using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Tests.Mocks
{
    /// <summary>
    /// Fixed-settings <see cref="ISettingsProvider"/> for tests. Registering it also keeps
    /// Program.cs from running the real <c>SettingsService.InitializeAsync()</c> (no SQLite
    /// files in test bins, no config import).
    /// </summary>
    public class StaticSettingsProvider(IServerSettings _settings) : ISettingsProvider
    {
        public IServerSettings Current => _settings;
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public void RaiseSettingsChanged(SettingsChangedEventArgs args) => SettingsChanged?.Invoke(this, args);
    }
}
