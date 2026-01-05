using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Services;

[TestFixture]
public class AccountOverrideMergerTests
{
    [Test]
    public void Apply_WhenOverridesAreNullLike_DoesNotChangeBase()
    {
        var baseAccount = new ServerAccountSettings
        {
            ImmichServerUrl = "http://mock",
            ApiKey = "k",
            ShowFavorites = false,
            ShowMemories = false,
            ShowArchived = false,
            ImagesFromDays = 7,
            Rating = 3,
            Albums = new(),
            ExcludedAlbums = new(),
            People = new()
        };

        var overrides = new AccountOverrideDto(); // all null
        AccountOverrideMerger.Apply(baseAccount, overrides);

        Assert.That(baseAccount.ShowFavorites, Is.False);
        Assert.That(baseAccount.ImagesFromDays, Is.EqualTo(7));
        Assert.That(baseAccount.Rating, Is.EqualTo(3));
    }

    [Test]
    public void Apply_WhenOverrideSetsEmptyLists_ClearsSelections()
    {
        var baseAccount = new ServerAccountSettings
        {
            ImmichServerUrl = "http://mock",
            ApiKey = "k",
            Albums = new() { Guid.NewGuid() },
            People = new() { Guid.NewGuid() }
        };

        var overrides = new AccountOverrideDto
        {
            Albums = new List<Guid>(),
            People = new List<Guid>()
        };

        AccountOverrideMerger.Apply(baseAccount, overrides);

        Assert.That(baseAccount.Albums, Is.Empty);
        Assert.That(baseAccount.People, Is.Empty);
    }
}


