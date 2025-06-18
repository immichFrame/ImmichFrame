using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class TotalAccountImagesSelectionStrategy(IAssetAccountTracker _tracker) : IAccountSelectionStrategy
{
    private IList<IImmichFrameLogic> _accounts;

    public void Initialize(IList<IImmichFrameLogic> accounts)
    {
        _accounts = accounts;
    }

    public async Task<AssetResponseDto?> GetNextAsset()
    {
        var chosen = await _accounts.ChooseOne(logic => logic.GetTotalAssets());


        var asset = await chosen.GetNextAsset();
        if (asset != null)
        {
            await _tracker.RecordAssetLocation(chosen, asset.Id);
            return asset;
        }

        return null;
    }

    private async Task<(IList<long>, long)> GetWeights(IList<IImmichFrameLogic> accounts)
    {
        var weights = await Task.WhenAll(accounts.Select(GetTotalForAccount));
        return (weights, weights.Sum());
    }

    private async Task<IList<double>> GetProportions(IList<IImmichFrameLogic> accounts)
    {
        var (totals, sum) = await GetWeights(accounts);
        return totals.Select(t => (double)t / sum).ToList();
    }

    private Task<long> GetTotalForAccount(IImmichFrameLogic account)
    {
        return account.GetTotalAssets();
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
    {
        var proportions = await GetProportions(_accounts);
        var maxAccount = proportions.Max();
        var adjustedProportions = proportions.Select(x => x / maxAccount).ToList();
        var assetLists = _accounts.Select(account => account.GetAssets()).ToList();

        var taskList = assetLists
            .Zip(_accounts, adjustedProportions)
            .Select(async tuple =>
            {
                var (task, account, proportion) = tuple;
                return (account, (await task).Shuffle().TakeProportional(proportion));
            });

        var accountAssetTupleList = await Task.WhenAll(taskList);

        foreach (var accountAssetTuple in accountAssetTupleList)
        {
            foreach (var asset in accountAssetTuple.Item2)
            {
                await _tracker.RecordAssetLocation(accountAssetTuple.account, asset.Id);
            }
        }

        return accountAssetTupleList.SelectMany(tuple => tuple.Item2);
    }

    public T ForAsset<T>(Guid assetId, Func<IImmichFrameLogic, T> f)
        => _tracker.ForAsset(assetId.ToString(), f);
}