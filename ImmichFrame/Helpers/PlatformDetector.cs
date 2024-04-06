using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public static class PlatformDetector
    {
        public static bool IsAndroid()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
        }
        public static bool IsDesktop()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
    }
}
