using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic;

public class TotalAccountImagesSelectionStrategy : IAccountSelectionStrategy
{
    private readonly Random _random = new();

    public async Task<(IImmichFrameLogic, AssetResponseDto)?> GetNextAsset(IList<IImmichFrameLogic> accounts)
    {
        var chosen = await accounts.ChooseOne(logic => logic.GetTotalAssets());

        var asset = await chosen.GetNextAsset();
        if (asset != null)
        {
            return (chosen, asset);
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