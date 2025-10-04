using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class ChronologicalAssetsPoolWrapper(IAssetPool basePool, IGeneralSettings generalSettings) : IAssetPool
{
    private readonly Random _random = new();
    
    public Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return basePool.GetAssetCount(ct);
    }
    
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        if (!generalSettings.ShowChronologicalImages || generalSettings.ChronologicalImagesCount <= 0)
        {
            // Fallback to base pool behavior if chronological is disabled
            return await basePool.GetAssets(requested, ct);
        }
        
        var chronologicalCount = generalSettings.ChronologicalImagesCount;
        
        // Get a larger set to work with for chronological selection
        var availableAssets = await basePool.GetAssets(Math.Max(requested * 10, 100), ct);
        
        // Create chronological sets and mix them randomly
        var chronologicalSets = CreateChronologicalSets(availableAssets, chronologicalCount);
        
        // Shuffle the sets randomly but keep chronological order within each set
        var shuffledSets = chronologicalSets.OrderBy(_ => _random.Next()).ToList();
        
        // Flatten the sets and take the requested amount
        var result = shuffledSets
            .SelectMany(set => set)
            .Take(requested)
            .ToList();
        
        return result;
    }
    
    private IEnumerable<IEnumerable<AssetResponseDto>> CreateChronologicalSets(IEnumerable<AssetResponseDto> allAssets, int setSize)
    {
        // Sort all assets by date
        var sortedAssets = allAssets
            .Where(a => a.ExifInfo?.DateTimeOriginal.HasValue == true)
            .OrderBy(a => a.ExifInfo.DateTimeOriginal!.Value)
            .ToList();
            
        if (!sortedAssets.Any())
        {
            // Fallback: create sets from randomly ordered assets if no date info available
            var randomAssets = allAssets.OrderBy(_ => _random.Next()).ToList();
            return CreateSetsFromList(randomAssets, setSize);
        }
        
        // Create consecutive chronological sets
        return CreateSetsFromList(sortedAssets, setSize);
    }
    
    private IEnumerable<IEnumerable<AssetResponseDto>> CreateSetsFromList(List<AssetResponseDto> assets, int setSize)
    {
        var sets = new List<List<AssetResponseDto>>();
        
        for (int i = 0; i < assets.Count; i += setSize)
        {
            var set = assets.Skip(i).Take(setSize).ToList();
            if (set.Any())
            {
                sets.Add(set);
            }
        }
        
        return sets;
    }
}