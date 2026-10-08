namespace ImmichFrame.Core.Interfaces
{
    public interface ISettingsProvider
    {
        IServerSettings Current { get; }
        event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        /// <summary>
        /// Bumps whenever what the slideshow shows (accounts, albums, people, tags...) changed or a
        /// refresh was requested. Clients compare it to drop their queued photos.
        /// </summary>
        long ContentRevision => 0;
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
