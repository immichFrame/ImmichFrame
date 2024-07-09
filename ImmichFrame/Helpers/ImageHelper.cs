using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ImmichFrame.Helpers
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
    }
}
