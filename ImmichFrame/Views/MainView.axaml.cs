using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using ImmichFrame.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

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
    private AssetInfo? LastAsset;
    private AssetInfo? CurrentAsset;
    public MainView()
    {
        InitializeComponent();
        _appSettings = Settings.CurrentSettings;
        _viewModel = new MainViewModel();
        _assetHelper = new AssetHelper();
        DataContext = _viewModel;
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            ShowSplash();

            if (_appSettings == null)
            {
                await ShowMessageBox("Error parsing Settings.xml. Check formatting.");
                ExitApp();
            }
            else
            {
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
        _viewModel.Image = bitmap;
    }

    private async void ShowNextImage()
    {
        try
        {
            if (timerImageSwitcher_Enabled)
            {
                LastAsset = CurrentAsset;
                CurrentAsset = _assetHelper.GetNextAsset();
                if (CurrentAsset != null)
                {
                    await SetNewImage(CurrentAsset);
                }
            }
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
                await SetNewImage(LastAsset);
                timerImageSwitcher_Enabled = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message);
        }
    }

    private async Task SetNewImage(AssetInfo asset)
    {
        using (Stream stream = await asset.AssetImage)
        {
            Bitmap bitmap = new Bitmap(stream);
            _viewModel.Image = bitmap;
            _viewModel.ImageDate = asset.FileCreatedAt.ToString(_appSettings.PhotoDateFormat);
        }
    }

    public void btnBack_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        ShowPreviousImage();
    }

    public void btnNext_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        ShowNextImage();
    }

    public void btnQuit_Click(object? sender, RoutedEventArgs args)
    {
        ExitApp();
    }

    private void ExitApp()
    {
        timerImageSwitcher_Enabled = false;
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

    private async Task ShowMessageBox(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("", message, ButtonEnum.Ok);
        await box.ShowAsPopupAsync(this);
    }
}
