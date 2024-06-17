using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Service.Dreams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmichFrame.Android
{
    [Service(Name = "com.immichframe.immichframe.ScreenSaverService", Permission = "android.permission.BIND_DREAM_SERVICE")]
    public class ScreenSaverService : DreamService
    {
        public override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            base.Fullscreen = true;
        }
        public override void OnDreamingStarted()
        {
            base.OnDreamingStarted();
            // Launch main activity when the screensaver starts
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.NewTask);
            StartActivity(intent);

            // Finish the dream (screensaver)
            Finish();
        }

        public override void OnDreamingStopped()
        {
            base.OnDreamingStopped();
        }
    }
}
