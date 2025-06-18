using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.AccountSelection;

public interface IAssetAccountTracker
{
    ValueTask<bool> RecordAssetLocation(IImmichFrameLogic account, string assetId);
    T ForAsset<T>(string assetId, Func<IImmichFrameLogic, T> f);
}