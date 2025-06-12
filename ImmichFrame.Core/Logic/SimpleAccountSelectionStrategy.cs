using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic;

public class SimpleAccountSelectionStrategy : IAccountSelectionStrategy
{
    private readonly Random _random = new();

    public async Task<(IImmichFrameLogic, AssetResponseDto)?> GetNextAsset(IList<IImmichFrameLogic> accounts)
    {
        var account = accounts[_random.Next(accounts.Count)];
        AssetResponseDto? asset = await account.GetNextAsset(); // Await the task directly

        if (asset == null)
        {
            return null;
        }
        
        return (account, asset);
    }

    public async Task<(IImmichFrameLogic account, IEnumerable<AssetResponseDto>)[]> GetAssets(IList<IImmichFrameLogic> accounts)
    {
        var tasks = accounts.Select(async account => (account, await account.GetAssets())).ToList(); 

        return await Task.WhenAll(tasks);
    }
}