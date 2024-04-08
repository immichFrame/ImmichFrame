using ImmichFrame.Models;

namespace ImmichFrame.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; set; }
    }
}
