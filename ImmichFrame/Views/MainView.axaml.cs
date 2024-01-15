using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImmichFrame.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace ImmichFrame.Views;

public partial class MainView : UserControl
{
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    private bool timerImageSwitcher_Enabled = false;
    private bool timerLiveTime_Enabled = true;
    MainViewModel viewModel = new MainViewModel();
    private string AccessToken = "";
    private AssetInfo? LastAsset;
    private AssetInfo? CurrentAsset;

    public class LoginData
    {
        public string? accessToken { get; set; }
    }
    private Settings? AppSettings;
    private class Settings
    {
        public string? ImmichServerUrl { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int Interval { get; set; }
        public bool ShowClock { get; set; }
        public int ClockFontSize { get; set; }
        public string? ClockFormat { get; set; }
        public bool ShowPhotoDate { get; set; }
        public int PhotoDateFontSize { get; set; }
        public string? PhotoDateFormat { get; set; }
    }
    public class AssetInfo
    {
        public string? id { get; set; }
        public DateTime fileCreatedAt { get; set; }
    }
    public MainView()
    {
        InitializeComponent();
        DataContext = viewModel;
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            ShowSplash();
            AppSettings = ParseSettings();
            if (AppSettings == null)
            {
                await ShowMessageBox("Error parsing Settings.xml. Check formatting.");
                ExitApp();
            }
            else
            {
                AccessToken = await Login();
                if (AccessToken == "")
                {
                    ExitApp();
                }
                else
                {
                    timerImageSwitcher_Enabled = true;
                    timerImageSwitcher = new System.Threading.Timer(timerImageSwitcherTick, null, 0, AppSettings.Interval * 1000);
                    if (AppSettings.ShowClock!)
                    {
                        timerLiveTime = new System.Threading.Timer(LiveTimeTick, null, 0, 1000);
                    }

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

    private void LiveTimeTick(object? state)
    {
        if (timerLiveTime_Enabled)
        {
            viewModel.LiveTime = DateTime.Now.ToString(AppSettings!.ClockFormat);
        }
    }
    private async Task<string> Login()
    {
        //string ret = "";
        HttpClient client = new HttpClient();
        string url = AppSettings!.ImmichServerUrl + "/api/auth/login";

        var payload = new
        {
            email = AppSettings.Email,
            password = AppSettings.Password
        };

        var jsonPayload = JsonSerializer.Serialize(payload);

        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginData = JsonSerializer.Deserialize<LoginData>(responseContent);
            if (loginData != null)
            {
                return loginData!.accessToken!;
            }
            else
            {
                throw new Exception("Unable to login to Immich server");
            }
        }
        else
        {
            throw new Exception(response.ToString());
        }
        //return ret;
    }
    private AssetInfo? GetRandomAsset()
    {
        AssetInfo returnAsset = new AssetInfo();
        string url = AppSettings!.ImmichServerUrl + "/api/asset/random";
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var Asset = JsonSerializer.Deserialize<List<AssetInfo>>(responseContent);
                if (Asset != null)
                {
                    returnAsset = Asset[0];
                }
            }
        }
        return returnAsset;
    }
    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        viewModel.Image = bitmap;
    }
    private async void ShowNextImage()
    {
        try
        {
            if (timerImageSwitcher_Enabled)
            {
                LastAsset = CurrentAsset;
                CurrentAsset = GetRandomAsset();
                if (CurrentAsset != null)
                {
                    string ImageURL = AppSettings!.ImmichServerUrl + "/api/asset/thumbnail/" + CurrentAsset.id + "?format=JPEG";
                    byte[] imageData = await DownloadImage(ImageURL);
                    using (MemoryStream stream = new MemoryStream(imageData))
                    {
                        Bitmap bitmap = new Bitmap(stream);
                        viewModel.Image = bitmap;
                        viewModel.ImageDate = CurrentAsset.fileCreatedAt.ToString(AppSettings.PhotoDateFormat);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message);
            ExitApp();
        }
    }
    private async void ShowPreviousImage()
    {
        try
        {
            if (LastAsset != null)
            {
                timerImageSwitcher_Enabled = false;
                string ImageURL = AppSettings!.ImmichServerUrl + "/api/asset/thumbnail/" + LastAsset.id + "?format=JPEG";
                byte[] imageData = await DownloadImage(ImageURL);
                using (MemoryStream stream = new MemoryStream(imageData))
                {
                    Bitmap bitmap = new Bitmap(stream);
                    viewModel.Image = bitmap;
                    viewModel.ImageDate = LastAsset.fileCreatedAt.ToString(AppSettings.PhotoDateFormat);
                }
                timerImageSwitcher_Enabled = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message);
            ExitApp();
        }
    }
    public void btnBack_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(AppSettings!.Interval * 1000, AppSettings.Interval * 1000);
        ShowPreviousImage();
    }

    public void btnNext_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(AppSettings!.Interval * 1000, AppSettings.Interval * 1000);
        ShowNextImage();
    }

    public void btnQuit_Click(object? sender, RoutedEventArgs args)
    {
        ExitApp();
    }

    private async Task<byte[]> DownloadImage(string ImageURL)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var data = await client.GetByteArrayAsync(ImageURL);
            return data;
        }
    }
    private void ExitApp()
    {
        timerLiveTime_Enabled = false;
        timerImageSwitcher_Enabled = false;
        Environment.Exit(0);
    }
    private Settings? ParseSettings()
    {
        var xml = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");
        var settings = new Settings
        {
            ImmichServerUrl = XElement.Parse(xml).Element("ImmichServerUrl")!.Value,
            Email = XElement.Parse(xml).Element("Email")!.Value,
            Password = XElement.Parse(xml).Element("Password")!.Value,
            Interval = int.Parse(XElement.Parse(xml).Element("Interval")!.Value),
            ShowClock = bool.Parse(XElement.Parse(xml).Element("ShowClock")!.Value),
            ClockFontSize = int.Parse(XElement.Parse(xml).Element("ClockFontSize")!.Value),
            ClockFormat = XElement.Parse(xml).Element("ClockFormat")!.Value,
            ShowPhotoDate = bool.Parse(XElement.Parse(xml).Element("ShowPhotoDate")!.Value),
            PhotoDateFontSize = int.Parse(XElement.Parse(xml).Element("PhotoDateFontSize")!.Value),
            PhotoDateFormat = XElement.Parse(xml).Element("PhotoDateFormat")!.Value,
        };
        viewModel.ShowClock = settings.ShowClock;
        viewModel.ClockFontSize = settings.ClockFontSize;
        viewModel.ShowPhotoDate = settings.ShowPhotoDate;
        viewModel.PhotoDateFontSize = settings.PhotoDateFontSize;
        return settings;
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

        var result = await box.ShowAsPopupAsync(this);
    }
}
