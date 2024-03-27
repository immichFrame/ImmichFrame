using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public static class ImageHelper
    {
        public static Stream SaveDataUrlToStream(string dataUrl)
        {
            var matchGroups = Regex.Match(dataUrl, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
            var base64Data = matchGroups["data"].Value;
            var binData = Convert.FromBase64String(base64Data);

            var ms = new MemoryStream(binData);

            return ms;
        }
    }
}
