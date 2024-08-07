using ImmichFrame.Helpers;
using System.Windows.Input;

namespace ImmichFrame.ViewModels
{
    internal class WelcomeViewModel : NavigatableViewModelBase
    {
        public ICommand SettingsCommand { get; set; }

        public WelcomeViewModel()
        {
            SettingsCommand = new RelayCommand(SettingsAction);
        }

        public void SettingsAction()
        {
            this.Navigate(new SettingsViewModel(false));
        }
    }
}
