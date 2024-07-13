using Android.App;
using Android.Content.PM;
using Android.Views;
using Avalonia;
using Avalonia.Android;

namespace ImmichFrame.Android;

[Activity(
    Label = "ImmichFrame",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/Immich",
    Name = "com.immichframe.immichframe.MainActivity",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
