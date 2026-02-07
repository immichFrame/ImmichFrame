namespace ImmichFrame.Core.Interfaces
{
    public interface IRequestContext
    {
        int AssetOffset { get; set; }
        int AssetShuffleRandom { get; set; }
    }
}
