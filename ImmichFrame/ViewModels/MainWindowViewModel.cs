using Avalonia.Controls;
using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using ImmichFrame.Views;

namespace ImmichFrame.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            try
            {
                var settings = Settings.CurrentSettings;

                _contentView = new MainView();
            }
            catch (SettingsNotValidException ex)
            {
                _contentView = new SettingsView(new Settings());
            }
        }

        public UserControl _contentView;
        public UserControl ContentView
        {
            get { return _contentView; }
            set
            {
                _contentView = value;
                OnPropertyChanged(nameof(ContentView));
            }
        }

        public void Navigate(UserControl view)
        {
            ContentView = view;
        }
    }
}
