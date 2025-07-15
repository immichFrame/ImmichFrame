using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.AccountSelection;

public interface IAssetAccountTracker
{
    ValueTask<bool> RecordAssetLocation(IAccountImmichFrameLogic account, string assetId);
    T ForAsset<T>(string assetId, Func<IAccountImmichFrameLogic, T> f);
}