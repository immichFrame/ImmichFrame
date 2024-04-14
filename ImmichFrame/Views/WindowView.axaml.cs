using Avalonia.Controls;
using ImmichFrame.ViewModels;

namespace ImmichFrame.Views
{
    public partial class WindowView : BaseView
    {
        public WindowView()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel();
        }
    }
}
