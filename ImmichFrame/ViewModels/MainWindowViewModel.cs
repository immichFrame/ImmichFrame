using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Models;

namespace ImmichFrame.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Thickness margin;

        public MainWindowViewModel()
        {
            try
            {
                var settings = Settings.CurrentSettings;

                Margin = Thickness.Parse(settings.Margin);

                ContentViewModel = new MainViewModel();
            }
            catch (SettingsNotValidException ex)
            {
                ContentViewModel = new ErrorViewModel(ex);

                Margin = new Thickness(0);
            }

            this.ContentViewModel.Navigated += Navigate;
        }

        [ObservableProperty]
        private NavigatableViewModelBase contentViewModel;

        private void Navigate(object? sender, NavigatedEventArgs e)
        {
            this.ContentViewModel.Navigated -= Navigate;
            ContentViewModel = e.ViewModel;
            this.ContentViewModel.Navigated += Navigate;
        }
    }
}
