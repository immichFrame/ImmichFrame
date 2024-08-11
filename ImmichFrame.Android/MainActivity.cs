using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using ImmichFrame.ViewModels;

namespace ImmichFrame.Android;

[Activity(
    Label = "ImmichFrame",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/AppIcon",
    Name = "com.immichframe.immichframe.MainActivity",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private static bool isAppRunning;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
        Window!.AddFlags(WindowManagerFlags.Fullscreen);
        isAppRunning = true;
    }
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
    protected override void OnStart()
    {
        base.OnStart();
        if (!isAppRunning)
        {
            isAppRunning = true;
            MainViewModel.Instance?.PauseImage();
        }
    }
    protected override void OnStop()
    {
        base.OnStop();
        if (isAppRunning)
        {
            isAppRunning = false;
            MainViewModel.Instance?.PauseImage();
        }
    }
}
