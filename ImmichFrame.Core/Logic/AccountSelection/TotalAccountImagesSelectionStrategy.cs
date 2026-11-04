using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class TotalAccountImagesSelectionStrategy(
    ILogger<TotalAccountImagesSelectionStrategy> _logger,
    IAssetAccountTracker _tracker,
    IList<IAccountImmichFrameLogic> _accounts) : IAccountSelectionStrategy
{
    // The batch size of the account that holds the most assets; the others are asked for their share of it.
    public const int BatchSize = 25;

    public async Task<(IAccountImmichFrameLogic, AssetResponseDto)?> GetNextAsset(string clientId = "", CancellationToken ct = default)
    {
        if (_accounts.Count == 0)
        {
            _logger.LogDebug("No accounts configured, returning no asset");
            return null;
        }

        var chosen = await _accounts.ChooseOne(logic => logic.GetTotalAssets());

        var asset = await chosen.GetNextAsset(clientId, ct);
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
        if (sum == 0)
        {
            // No account reported any assets — avoid 0/0 NaN proportions; uniform weights
            // keep the (empty) batches intact.
            return accounts.Select(_ => 1d).ToList();
        }

        return totals.Select(t => (double)t / sum).ToList();
    }

    private Task<long> GetTotalForAccount(IImmichFrameLogic account)
    {
        return account.GetTotalAssets();
    }

    public async Task<IEnumerable<(IAccountImmichFrameLogic, AssetResponseDto)>> GetAssets(string clientId = "", CancellationToken ct = default)
    {
        if (_accounts.Count == 0)
        {
            _logger.LogDebug("No accounts configured, returning no assets");
            return Enumerable.Empty<(IAccountImmichFrameLogic, AssetResponseDto)>();
        }

        var proportions = await GetProportions(_accounts);
        var maxAccount = proportions.Max();
        var adjustedProportions = proportions.Select(x => x / maxAccount).ToList();

        // Ask each account only for its share, so nothing dealt from an account's deck is discarded.
        var taskList = _accounts
            .Zip(adjustedProportions)
            .Select(async tuple =>
            {
                var (account, proportion) = tuple;
                var count = proportion > 0 ? (int)Math.Ceiling(BatchSize * proportion) : 0;
                if (count == 0)
                {
                    return (account, assets: (IEnumerable<AssetResponseDto>)[]);
                }

                var assets = (await account.GetAssets(count, clientId, ct)).ToList();
                _logger.LogDebug("Retrieved {total} asset(s) for account [{account}], asked for {count}", assets.Count, account, count);
                return (account, assets: (IEnumerable<AssetResponseDto>)assets);
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
        => _tracker.ForAsset(assetId, f);
}