using Avalonia.Controls;
using Avalonia.Interactivity;
using ImmichFrame.Models;
using ImmichFrame.ViewModels;

namespace ImmichFrame.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(Settings settings)
        {
            InitializeComponent();

            this.DataContext = new SettingsViewModel(settings);
        }

        public void btnSave_Click(object? sender, RoutedEventArgs args)
        {
            (DataContext as SettingsViewModel).Settings.Serialize();

            ((MainWindowViewModel)this.Parent.DataContext).Navigate(new MainView());
        }
    }
}
