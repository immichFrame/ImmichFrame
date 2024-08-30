using System.Text.Json.Serialization;
using ImmichFrame.Core.Helpers;
using ThumbHashes;

namespace ImmichFrame.Core.Api
{
    public partial class AssetResponseDto
    {
        [JsonIgnore]
        private string? _imageDesc;

        [JsonIgnore]
        public string ImageDesc
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_imageDesc))
                    return this.ExifInfo?.Description ?? string.Empty;

                return _imageDesc;
            }
            set
            {
                _imageDesc = value;
            }
        }

        [JsonIgnore]
        public Stream? ThumbhashImage => GetThumbHashStream();

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
