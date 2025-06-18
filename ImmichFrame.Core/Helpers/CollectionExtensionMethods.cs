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
        => source.Where(item1 => !excluded.Any(item2 => Equals(comparator(item2), comparator(item1))));

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
    
    public static async Task<TValue> GetOrCreateAsync<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, Task<TValue>> createNew)
    {
        if (!dict.TryGetValue(key, out var val))
        {
            val = await createNew(key);
            dict.Add(key, val);
        }

        return val;
    }
}