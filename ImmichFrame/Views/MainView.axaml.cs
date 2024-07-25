using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using ImmichFrame.ViewModels;
using System;

namespace ImmichFrame.Views;

public partial class MainView : BaseView
{
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    System.Threading.Timer? timerWeather;
    private MainViewModel? _viewModel;
    private Settings? _appSettings;

    public MainView()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (PlatformDetector.IsAndroid())
        {
            var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
            if (insetsManager != null)
            {
                insetsManager.DisplayEdgeToEdge = true;
                insetsManager.IsSystemBarVisible = false;
            }
        }

        _viewModel = (this.DataContext as MainViewModel)!;

        try
        {
            _viewModel.ResetTimer += ResetTimer;

            ShowSplash();

            _appSettings = _viewModel.Settings;

            if (_appSettings == null)
                throw new SettingsNotValidException("Settings could not be parsed.");


            if (transitioningControl.PageTransition is CrossFade crossFade)
            {
                crossFade.Duration = TimeSpan.FromSeconds(_appSettings.TransitionDuration);
            }
            AssetHelper _assetHelper = new AssetHelper();
            if(_appSettings.ImmichFrameAlbumName)
                await _assetHelper.DeleteAndCreateImmichFrameAlbum();
            await _viewModel.ShowNextImage();

            _viewModel.TimerEnabled = true;
            timerImageSwitcher = new System.Threading.Timer(_viewModel.NextImageTick, null, 0, _appSettings.Interval * 1000);
            if (_appSettings.ShowClock)
            {
                timerLiveTime = new System.Threading.Timer(_viewModel.LiveTimeTick, null, 0, 1 * 1000); //every second
            }
            if (_appSettings.ShowWeather)
            {
                timerWeather = new System.Threading.Timer(_viewModel.WeatherTick, null, 0, 10 * 60 * 1000); //every 10 minutes
            }
        }
        catch (Exception ex)
        {
            this._viewModel.Navigate(new ErrorViewModel(ex));
        }
    }

    private void ResetTimer(object? sender, EventArgs e)
    {
        timerImageSwitcher?.Change(_appSettings!.Interval * 1000, _appSettings!.Interval * 1000);
    }

    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        _viewModel?.SetImage(bitmap);
    }

    public void btnQuit_Click(object? sender, RoutedEventArgs args)
    {
        ExitApp();
    }

    public override void Dispose()
    {
        if (_viewModel != null)
            _viewModel.TimerEnabled = false;
        timerImageSwitcher?.Dispose();
        timerLiveTime?.Dispose();
        timerWeather?.Dispose();

        base.Dispose();
    }
    private void ExitApp()
    {
        this.Dispose();
        Environment.Exit(0);
    }
}
