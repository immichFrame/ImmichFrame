using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class TotalAccountImagesSelectionStrategy(ILogger<TotalAccountImagesSelectionStrategy> _logger, IAssetAccountTracker _tracker, IGeneralSettings _generalSettings) : IAccountSelectionStrategy
{
    private IList<IAccountImmichFrameLogic> _accounts;

    public void Initialize(IList<IAccountImmichFrameLogic> accounts)
    {
        _accounts = accounts;
    }

    public async Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset()
    {
        var chosen = await _accounts.ChooseOne(logic => logic.GetTotalAssets());
        
        var asset = await chosen.GetNextAsset();
        if (asset != null)
        {
            await _tracker.RecordAssetLocation(chosen, asset.Id);
            return (chosen, asset);
        }
        
        _logger.LogDebug("No next asset found");
        return null;
    }

    private async Task<(IList<long>, long)> GetWeights(IList<IAccountImmichFrameLogic> accounts)
    {
        var weights = await Task.WhenAll(accounts.Select(GetTotalForAccount));
        return (weights, weights.Sum());
    }

    private async Task<IList<double>> GetProportions(IList<IAccountImmichFrameLogic> accounts)
    {
        var (totals, sum) = await GetWeights(accounts);
        return totals.Select(t => (double)t / sum).ToList();
    }

    private Task<long> GetTotalForAccount(IImmichFrameLogic account)
    {
        return account.GetTotalAssets();
    }

    public async Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets()
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
                var assets = (await task).ToList();
                _logger.LogDebug("Retrieved {total} asset(s) for account [{account}], will take {proportion}%", assets.Count(), account, proportion * 100);
                
                // Skip shuffling if chronological sorting is enabled to preserve order
                var processedAssets = _generalSettings.ChronologicalImagesCount > 0 
                    ? assets.TakeProportional(proportion)
                    : assets.Shuffle().TakeProportional(proportion); // Better would be to shuffle sets, in pool classes;
                return (account, processedAssets);
            });

        var accountAssetTupleList = await Task.WhenAll(taskList);

        _logger.LogDebug("Processing {} list(s) of asset(s) of length {}", accountAssetTupleList.Length, string.Join(",", accountAssetTupleList.Select(a => a.Item2.Count())));

        foreach (var accountAssetTuple in accountAssetTupleList)
        {
            foreach (var asset in accountAssetTuple.Item2)
            {
                await _tracker.RecordAssetLocation(accountAssetTuple.account, asset.Id);
            }
        }

        var assets = accountAssetTupleList.SelectMany(tuple => tuple.Item2.Select(asset => (tuple.account, asset))).ToList();

        _logger.LogDebug("Returning {count} asset(s)", assets.Count);

        return assets;
    }

    public T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f)
        => _tracker.ForAsset(assetId.ToString(), f);
}