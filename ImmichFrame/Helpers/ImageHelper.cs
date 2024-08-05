using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public static async Task<Bitmap?> LoadImageFromWeb(Uri url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            }
            catch
            {
                return null;
            }
        }
    }
}
