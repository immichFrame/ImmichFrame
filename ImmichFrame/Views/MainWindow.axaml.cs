using Avalonia.Controls;
using FluentAvalonia.UI.Windowing;

namespace ImmichFrame.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        this.WindowState = WindowState.FullScreen;
    }
}
