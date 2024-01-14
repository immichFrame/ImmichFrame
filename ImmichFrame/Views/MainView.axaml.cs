using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImmichFrame.ViewModels;

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
        public string? immichUrl { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public int interval { get; set; }
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
        ShowSplash();
        AppSettings = ParseSettings();
        //image switcher timer
        timerImageSwitcher = new System.Threading.Timer(timerImageSwitcherTick, null, 0, AppSettings.interval * 1000);
        //Clock timer
        timerLiveTime = new System.Threading.Timer(LiveTimeTick, null, 0, 1000);
        AccessToken = await Login();
        timerImageSwitcher_Enabled = true;
        //ShowNextImage();
    }

    private void timerImageSwitcherTick(object? state)
    {
        ShowNextImage();
    }

    private void LiveTimeTick(object? state)
    {
        if (timerLiveTime_Enabled)
        {
            viewModel.LiveTime = DateTime.Now.ToString("h:mm tt");
        }
    }
    private async Task<string> Login()
    {
        string ret = "";
        HttpClient client = new HttpClient();
        string url = AppSettings!.immichUrl + "/api/auth/login";

        var payload = new
        {
            AppSettings.email,
            AppSettings.password
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
                ret = loginData.accessToken!;
            }
        }
        return ret;
    }
    private AssetInfo? GetRandomAsset()
    {
        string url = AppSettings!.immichUrl + "/api/asset/random";
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            var response = client.GetAsync(url).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var Asset = JsonSerializer.Deserialize<List<AssetInfo>>(responseContent);
            if (Asset != null)
            {
                return Asset[0];
            }
            else
            {
                return null;
            }
        }
    }
    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        viewModel.Image = bitmap;
    }
    private async void ShowNextImage()
    {
        if (timerImageSwitcher_Enabled)
        {
            LastAsset = CurrentAsset;
            CurrentAsset = GetRandomAsset();
            if (CurrentAsset != null)
            {
                string ImageURL = AppSettings!.immichUrl + "/api/asset/thumbnail/" + CurrentAsset.id + "?format=JPEG";
                byte[] imageData = await DownloadImage(ImageURL);
                using (MemoryStream stream = new MemoryStream(imageData))
                {
                    Bitmap bitmap = new Bitmap(stream);
                    viewModel.Image = bitmap;
                    viewModel.ImageDate = CurrentAsset.fileCreatedAt.ToString("MM/dd/yyyy");
                }
            }
        }
    }
    private async void ShowPreviousImage()
    {
        if (LastAsset != null)
        {
            timerImageSwitcher_Enabled = false;
            string ImageURL = AppSettings!.immichUrl + "/api/asset/thumbnail/" + LastAsset.id + "?format=JPEG";
            byte[] imageData = await DownloadImage(ImageURL);
            using (MemoryStream stream = new MemoryStream(imageData))
            {
                Bitmap bitmap = new Bitmap(stream);
                viewModel.Image = bitmap;
                viewModel.ImageDate = LastAsset.fileCreatedAt.ToString("MM/dd/yyyy");
            }
            timerImageSwitcher_Enabled = true;
        }
    }
    public void btnBack_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(AppSettings!.interval * 1000, AppSettings.interval * 1000);
        ShowPreviousImage();
    }

    public void btnNext_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(AppSettings!.interval * 1000, AppSettings.interval * 1000);
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
    private Settings ParseSettings()
    {
        Settings pss = new Settings();
        string[] FileContents = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Settings.txt");
        string[] Line1 = FileContents[0].Split('=');
        string[] Line2 = FileContents[1].Split('=');
        string[] Line3 = FileContents[2].Split('=');
        string[] Line4 = FileContents[3].Split('=');
        pss.immichUrl = Line1[1];
        pss.email = Line2[1];
        pss.password = Line3[1];
        pss.interval = int.Parse(Line4[1]);
        return pss;
    }

}
