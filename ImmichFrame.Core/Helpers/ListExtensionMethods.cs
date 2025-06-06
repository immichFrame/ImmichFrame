namespace ImmichFrame.Core.Helpers;

public static class ListExtensionMethods
{
    public static IEnumerable<T> TakeProportional<T>(this IEnumerable<T> enumerable, double proportion)
    {
        if (proportion <= 0) return [];

        var list = enumerable.ToList();
        var itemsToTake = (int) Math.Ceiling(list.Count * proportion);
        return list.Take(itemsToTake);
    }
}