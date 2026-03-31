using System.Text.RegularExpressions;

namespace ImmichFrame.Core.Helpers
{
    public static partial class ImageHelper
    {
        [GeneratedRegex(@"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$")]
        private static partial Regex DataRegex();
        
        public static Stream SaveDataUrlToStream(string dataUrl)
        {
            var base64Data = DataRegex().Match(dataUrl).Groups["data"].Value;
            var binData = Convert.FromBase64String(base64Data);

            return new MemoryStream(binData);
        }
        
        public static double GetAspectRatioFloat(double width, double height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive integers.");
        
            return (double)width / height;
        }

        public static bool IsLandscape(double exifWidth, double exifHeight, string exifOrientation)
        {
            double width;
            double height;
            if (exifOrientation == "1" 
                || exifOrientation == "2"
                || exifOrientation == "3"
                || exifOrientation == "4")
            {
                width = exifWidth;
                height = exifHeight;
            }else if (exifOrientation == "5" 
                      || exifOrientation == "6"
                      || exifOrientation == "7"
                      || exifOrientation == "8")
            {
                height = exifWidth;
                width = exifHeight;
            }
            else
            {
                throw new ArgumentException(nameof(exifOrientation));
            }

            return GetAspectRatioFloat(width, height) > 1;
        }
    }
}
