using System.Text.Json.Serialization;
using ImmichFrame.Core.Helpers;
using ThumbHashes;

namespace ImmichFrame.Core.Api
{
    public partial class AssetResponseDto
    {
        [JsonIgnore]
        public Stream? ThumbhashImage => GetThumbHashStream();
        
        public string ImmichServerUrl { get; set; }

        private Stream? GetThumbHashStream()
        {
            if (this.Thumbhash == null)
                return null;

            var hash = Convert.FromBase64String(this.Thumbhash);
            var thumbhash = new ThumbHash(hash);
            return ImageHelper.SaveDataUrlToStream(thumbhash.ToDataUrl());
        }
    }
}
