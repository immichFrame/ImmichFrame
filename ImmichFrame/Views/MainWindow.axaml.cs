using Avalonia.Controls;

namespace ImmichFrame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.WindowState = WindowState.FullScreen;
    }
}
