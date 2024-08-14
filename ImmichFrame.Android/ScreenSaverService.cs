using Android.App;
using Android.Content;
using Android.OS;
using Android.Service.Dreams;

namespace ImmichFrame.Android
{
    [Service(Name = "com.immichframe.immichframe.ScreenSaverService", Permission = "android.permission.BIND_DREAM_SERVICE")]
    public class ScreenSaverService : DreamService
    {
        private PowerManager.WakeLock? _wakeLock;
        public override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            // Try to get a WakeLock
            try
            {
                PowerManager powerManager = (PowerManager)GetSystemService(Context.PowerService)!;
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup, "ImmichFrame::ScreenSaverWakeLock");
                if (_wakeLock != null)
                {
                    _wakeLock.Acquire();
                }
            }
            catch
            {
                //couldn't get a WakeLock, just continue
            }
        }
        public override void OnDreamingStarted()
        {
            base.OnDreamingStarted();
            // Launch main activity when the screensaver starts
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ReorderToFront | ActivityFlags.NewTask);
            StartActivity(intent);
        }

        public override void OnDreamingStopped()
        {
            base.OnDreamingStopped();
            ReleaseWakeLock();
        }

        public override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            ReleaseWakeLock();
        }

        private void ReleaseWakeLock()
        {
            if (_wakeLock != null && _wakeLock.IsHeld)
            {
                _wakeLock.Release();
                _wakeLock = null;
            }
        }
    }
}
