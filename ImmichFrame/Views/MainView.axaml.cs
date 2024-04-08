using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using ImmichFrame.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Threading.Tasks;

namespace ImmichFrame.Views;

public partial class MainView : UserControl
{
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    System.Threading.Timer? timerWeather;
    private bool timerImageSwitcher_Enabled = false;
    private MainViewModel _viewModel;
    private Settings _appSettings;
    private AssetHelper _assetHelper;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    public MainView()
    {
        InitializeComponent();
        _appSettings = new Settings();
        _viewModel = new MainViewModel();
        _assetHelper = new AssetHelper();
        DataContext = _viewModel;
        this.Loaded += OnLoaded;
    }
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
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
            ShowSplash();

            _appSettings = Settings.CurrentSettings;
            _viewModel.Settings = _appSettings;

            if (transitioningControl.PageTransition is CrossFade crossFade)
            {
                crossFade.Duration = TimeSpan.FromSeconds(_appSettings.TransitionDuration);
            }

            timerImageSwitcher_Enabled = true;
            timerImageSwitcher = new System.Threading.Timer(timerImageSwitcherTick, null, 0, _appSettings.Interval * 1000);
            if (_appSettings.ShowClock)
            {
                timerLiveTime = new System.Threading.Timer(timerLiveTimeTick, null, 0, 1000); //every second
            }
            if (_appSettings.ShowWeather)
            {
                timerWeather = new System.Threading.Timer(timerWeatherTick, null, 0, 600000); //every 10 minutes
            }
        }
        catch (SettingsNotValidException ex)
        {
            await ShowMessageBox(ex.Message, "There was a Problem loading the Settings");
            ExitApp();
        }
        catch (Exception ex)
        {
            await ShowMessageBox(ex.Message);
            ExitApp();
        }
    }

    private void timerImageSwitcherTick(object? state)
    {
        ShowNextImage();
    }

    private void timerLiveTimeTick(object? state)
    {
        _viewModel.LiveTime = DateTime.Now.ToString(_appSettings.ClockFormat);
    }
    private void timerWeatherTick(object? state)
    {
        string latitude = _appSettings.WeatherLatLong!.Split(',')[0];
        string longitude = _appSettings.WeatherLatLong!.Split(',')[1];
        OpenMeteoResponse? openMeteoResponse = Task.Run(() => Weather.GetWeather(latitude, longitude, _appSettings.WeatherUnits!)).Result;
        if (openMeteoResponse != null)
        {
            _viewModel.WeatherTemperature = openMeteoResponse.current_weather!.temperature.ToString() + openMeteoResponse.current_weather_units!.temperature;
            string description = WmoWeatherInterpreter.GetWeatherDescription(openMeteoResponse.current_weather.weathercode, Convert.ToBoolean(openMeteoResponse.current_weather.is_day));
            _viewModel.WeatherCurrent = description;
        }
    }

    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        _viewModel.SetImage(bitmap);
    }

    private async void ShowNextImage()
    {
        try
        {
            if (timerImageSwitcher_Enabled)
            {
                LastAsset = CurrentAsset;
                CurrentAsset = await _assetHelper.GetNextAsset();
                if (CurrentAsset != null)
                {
                    await _viewModel.SetImage(CurrentAsset);
                }
            }
        }
        catch (AssetNotFoundException)
        {
            // Do not show message
        }
        catch (Exception ex)
        {
            timerImageSwitcher_Enabled = false;
            await ShowMessageBoxFromThread(ex.Message);
            timerImageSwitcher_Enabled = true;
        }
    }

    private async void ShowPreviousImage()
    {
        try
        {
            if (LastAsset != null)
            {
                timerImageSwitcher_Enabled = false;
                await _viewModel.SetImage(LastAsset);
                timerImageSwitcher_Enabled = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message);
        }
    }

    public async void btnBack_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        await Task.Run(() => ShowPreviousImage());
    }

    public async void btnNext_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        await Task.Run(() => ShowNextImage());
    }

    public void btnQuit_Click(object? sender, RoutedEventArgs args)
    {
        ExitApp();
    }
    public void btnSettings_Click(object? sender, RoutedEventArgs args)
    {
        ExitView();
        ((MainWindowViewModel)this.Parent.DataContext).Navigate(new SettingsView(Settings.CurrentSettings));
    }

    private void ExitView()
    {
        timerImageSwitcher_Enabled = false;
        timerImageSwitcher?.Dispose();
        timerLiveTime?.Dispose();
        timerWeather?.Dispose();
    }
    private void ExitApp()
    {
        ExitView();
        Environment.Exit(0);
    }
    private Task ShowMessageBoxFromThread(string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        Dispatcher.UIThread.Post(async () =>
            {
                await ShowMessageBox(message);
                tcs.SetResult(true);
            });
        return tcs.Task;
    }

    private async Task ShowMessageBox(string message, string title = "")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
        await box.ShowAsPopupAsync(this);
    }
}
