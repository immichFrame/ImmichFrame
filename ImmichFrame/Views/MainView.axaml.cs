using Avalonia.Controls;
using Avalonia.Interactivity;
using ImmichFrame.Helpers;
using ImmichFrame.ViewModels;
using System;
using System.Threading.Tasks;

namespace ImmichFrame.Views;

public partial class MainView : BaseView
{
    private MainViewModel? _viewModel;

    public MainView()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {       
        if(PlatformDetector.IsAndroid())
        {
            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            if (insetsManager != null)
            {
                insetsManager.DisplayEdgeToEdge = true;
                insetsManager.IsSystemBarVisible = false;
            }
        }

        _viewModel = (this.DataContext as MainViewModel)!;
        await InitializeViewModelAsync();
    }
    private async Task InitializeViewModelAsync()
    {
        try
        {
            await _viewModel!.InitializeAsync();
        }
        catch (Exception ex)
        {
            _viewModel!.Navigate(new ErrorViewModel(ex));
        }
    }   

    public override void Dispose()
    {
        if (_viewModel != null)
            _viewModel.TimerEnabled = false;

        base.Dispose();
    }   
}
