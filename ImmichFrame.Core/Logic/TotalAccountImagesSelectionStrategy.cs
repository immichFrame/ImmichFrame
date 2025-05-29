using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic;

using Microsoft.Extensions.Caching.Memory;

public class TotalAccountImagesSelectionStrategy : IAccountSelectionStrategy
{
    private readonly Random _random = new();
    private readonly IMemoryCache _accountToTotal = new MemoryCache(new MemoryCacheOptions());

    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));

    public async Task<(IImmichFrameLogic, AssetResponseDto)?> GetNextAsset(IList<IImmichFrameLogic> accounts)
    {
        var (weights, sum) = await GetWeights(accounts);
        var randomNumber = _random.Next(sum);
        var selectedIndex = accounts.Count - 1;

        for (var i = 0; i < accounts.Count; i++)
        {
            randomNumber -= weights[i];
            if (randomNumber <= 0)
            {
                selectedIndex = i;
                break;
            }
        }

        var asset = await accounts[selectedIndex].GetNextAsset();
        if (asset != null)
        {
            return (accounts[selectedIndex], asset);
        }
        else
        {
            return null;
        }
    }

    private async Task<(IList<int>, int)> GetWeights(IList<IImmichFrameLogic> accounts)
    {
        var weights = await Task.WhenAll(accounts.Select(a => GetTotalForAccount(a)));
        return (weights, weights.Sum());
    }

    private async Task<IList<double>> GetProportions(IList<IImmichFrameLogic> accounts)
    {
        var (totals, sum) = await GetWeights(accounts);
        return totals.Select(t => (double)t / sum).ToList();
    }

    private Task<int> GetTotalForAccount(IImmichFrameLogic account)
    {
        return _accountToTotal.GetOrCreateAsync(account, async entry => (await account.GetAssetStats()).Images,
            _memoryCacheEntryOptions);
    }

    public async Task<(IImmichFrameLogic account, IEnumerable<AssetResponseDto>)[]> GetAssets(
        IList<IImmichFrameLogic> accounts)
    {
        var proportions = await GetProportions(accounts);
        var maxAccount = proportions.Max();
        var adjustedProportions = proportions.Select(x => x / maxAccount).ToList();
        var assetLists = accounts.Select(account => account.GetAssets()).ToList();

        var taskList = assetLists
            .Zip(accounts, adjustedProportions)
            .Select(async tuple =>
            {
                var (task, account, proportion) = tuple;
                return (account, (await task).OrderBy(_ => _random.Next()).TakeProportional(proportion));
            });

        return await Task.WhenAll(taskList);
    }
}