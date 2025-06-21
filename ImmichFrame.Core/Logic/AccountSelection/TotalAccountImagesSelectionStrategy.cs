using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class TotalAccountImagesSelectionStrategy(ILogger<TotalAccountImagesSelectionStrategy> _logger, IAssetAccountTracker _tracker) : IAccountSelectionStrategy
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
            _logger.LogDebug("Returning next asset {id}", asset.Id);
            await _tracker.RecordAssetLocation(chosen, asset.Id);
            return asset;
        }
        
        _logger.LogDebug("No next asset found");
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
        _logger.LogDebug("Account [{}] will be split by proportion of {}", accounts, sum);
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
                var assets = (await task).ToList();
                _logger.LogDebug("Retrieved {total} asset(s) for account [{account}], will take {proportion}%", assets.Count(), account, proportion * 100);
                return (account, assets.Shuffle().TakeProportional(proportion));
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

        var assets = accountAssetTupleList.SelectMany(tuple => tuple.Item2).ToList();

        _logger.LogDebug("Returning {count} asset(s)", assets.Count);

        return assets;
    }

    public T ForAsset<T>(Guid assetId, Func<IImmichFrameLogic, T> f)
        => _tracker.ForAsset(assetId.ToString(), f);
}