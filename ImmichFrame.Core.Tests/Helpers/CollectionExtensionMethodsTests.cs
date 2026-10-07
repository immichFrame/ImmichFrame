using ImmichFrame.Core.Helpers;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class CollectionExtensionMethodsTests
{
    private record Item(Guid Id, string Name);

    [Test]
    public void WhereExcludes_ExcludesByComparator_NotByInstance()
    {
        var keep = new Item(Guid.NewGuid(), "keep");
        var drop = new Item(Guid.NewGuid(), "drop");
        // A different instance carrying the same Id must still be excluded.
        var excluded = new[] { new Item(drop.Id, "same id, other instance") };

        var result = new[] { keep, drop }.WhereExcludes(excluded, i => i.Id).ToList();

        Assert.That(result, Is.EqualTo(new[] { keep }));
    }

    [Test]
    public void WhereExcludes_EmptyExcluded_ReturnsEverything()
    {
        var source = new[] { new Item(Guid.NewGuid(), "a"), new Item(Guid.NewGuid(), "b") };

        var result = source.WhereExcludes([], i => i.Id).ToList();

        Assert.That(result, Is.EqualTo(source));
    }

    [Test]
    public void WhereExcludes_EnumeratedTwice_ReturnsSameResult()
    {
        var keep = new Item(Guid.NewGuid(), "keep");
        var drop = new Item(Guid.NewGuid(), "drop");

        var result = new[] { keep, drop }.WhereExcludes([drop], i => i.Id);

        Assert.That(result.ToList(), Is.EqualTo(new[] { keep }));
        Assert.That(result.ToList(), Is.EqualTo(new[] { keep }));
    }

    [Test]
    public void WhereExcludes_EnumeratesExcludedOnce_NotPerSourceItem()
    {
        var excluded = Enumerable.Range(0, 50).Select(_ => new Item(Guid.NewGuid(), "x")).ToList();
        var source = Enumerable.Range(0, 100).Select(_ => new Item(Guid.NewGuid(), "y")).ToList();

        var enumerations = 0;

        IEnumerable<Item> CountingExcluded()
        {
            enumerations++;
            foreach (var item in excluded)
            {
                yield return item;
            }
        }

        var result = source.WhereExcludes(CountingExcluded(), i => i.Id).ToList();

        Assert.That(result, Has.Count.EqualTo(100));
        Assert.That(enumerations, Is.EqualTo(1));
    }
}
