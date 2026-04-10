namespace ImmichFrame.Core.Api
{
    public partial class AssetListResponseDto
    {
        public int AssetOffset { get; set; }
        public List<AssetResponseDto>? Assets { get; set; }
    }
}
