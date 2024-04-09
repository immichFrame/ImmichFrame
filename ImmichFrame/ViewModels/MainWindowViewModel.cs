using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using System;
using static ImmichFrame.ViewModels.NavigatableViewModelBase;

namespace ImmichFrame.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            try
            {
                var settings = Settings.CurrentSettings;

                ContentViewModel = new MainViewModel();
            }
            catch (SettingsNotValidException ex)
            {
                ContentViewModel = new SettingsViewModel();
            }

            this.ContentViewModel.Navigated += Navigate;
        }

        [ObservableProperty]
        private NavigatableViewModelBase contentViewModel;

        public void Navigate(object sender, NavigatedEventArgs e)
        {
            this.ContentViewModel.Navigated -= Navigate;
            ContentViewModel = e.ViewModel;
            this.ContentViewModel.Navigated += Navigate;
        }
    }
}
