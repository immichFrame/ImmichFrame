namespace ImmichFrame.Core.Helpers;

public static class CollectionExtensionMethods
{
    private static readonly Random _random = new();
    
    public static IEnumerable<T> TakeProportional<T>(this IEnumerable<T> enumerable, double proportion)
    {
        if (proportion <= 0) return [];

        var list = enumerable.ToList();
        var itemsToTake = (int)Math.Ceiling(list.Count * proportion);
        return list.Take(itemsToTake);
    }

    public static IEnumerable<T> WhereExcludes<T>(this IEnumerable<T> source, IEnumerable<T> excluded)
        => WhereExcludes(source, excluded, t => t!);

    public static IEnumerable<T> WhereExcludes<T>(this IEnumerable<T> source, IEnumerable<T> excluded, Func<T, object> comparator)
    {
        // Hash the excluded keys once rather than rescanning `excluded` for every
        // item in `source`. The previous Any() form was O(n*m), which is fine for a
        // handful of excluded assets but stalls the asset pool when an excluded
        // album holds thousands.
        var excludedKeys = excluded.Select(comparator).ToHashSet();

        return source.Where(item => !excludedKeys.Contains(comparator(item)));
    }

    public static async Task<T?> ChooseOne<T>(this IEnumerable<T> sources, Func<T, Task<long>> probabilitySelector)
    {
        var sourcesAndCounts = await Task.WhenAll(
            sources.Select(async source => (Source: source, Count: await probabilitySelector(source)))
                .ToList());

        var totalCount = sourcesAndCounts.Sum(source => source.Count);

        var randomIndex = _random.NextInt64(totalCount);

        foreach (var sourceAndCount in sourcesAndCounts)
        {
            if (randomIndex < sourceAndCount.Count)
            {
                return sourceAndCount.Source;
            }

            randomIndex -= sourceAndCount.Count;
        }

        return default;
    }
    
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(_ => _random.Next());
}