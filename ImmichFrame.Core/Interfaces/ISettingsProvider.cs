namespace ImmichFrame.Core.Interfaces
{
    public interface ISettingsProvider
    {
        IServerSettings Current { get; }
        event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    }

    public class SettingsChangedEventArgs : EventArgs
    {
        public required IServerSettings NewSettings { get; init; }
        // Accounts list changed or RefreshAlbumPeopleInterval changed — the account
        // logic graph (pools, caches) must be rebuilt for these to take effect.
        public bool AccountsChanged { get; init; }
        public bool GeneralChanged { get; init; }
    }
}
