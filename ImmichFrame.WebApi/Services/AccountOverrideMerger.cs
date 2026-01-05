using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

internal static class AccountOverrideMerger
{
    public static void Apply(IAccountSettings target, AccountOverrideDto overrides)
    {
        if (overrides.ShowMemories is bool showMemories) Set(target, nameof(IAccountSettings.ShowMemories), showMemories);
        if (overrides.ShowFavorites is bool showFavorites) Set(target, nameof(IAccountSettings.ShowFavorites), showFavorites);
        if (overrides.ShowArchived is bool showArchived) Set(target, nameof(IAccountSettings.ShowArchived), showArchived);

        if (overrides.ImagesFromDays is int days) Set(target, nameof(IAccountSettings.ImagesFromDays), days);
        if (overrides.ImagesFromDate is DateTime fromDate) Set(target, nameof(IAccountSettings.ImagesFromDate), fromDate);
        if (overrides.ImagesUntilDate is DateTime untilDate) Set(target, nameof(IAccountSettings.ImagesUntilDate), untilDate);

        if (overrides.Albums != null) Set(target, nameof(IAccountSettings.Albums), overrides.Albums);
        if (overrides.ExcludedAlbums != null) Set(target, nameof(IAccountSettings.ExcludedAlbums), overrides.ExcludedAlbums);
        if (overrides.People != null) Set(target, nameof(IAccountSettings.People), overrides.People);

        if (overrides.Rating is int rating) Set(target, nameof(IAccountSettings.Rating), rating);
    }

    private static void Set(IAccountSettings target, string propertyName, object? value)
    {
        // Base settings classes in this repo are mutable concrete types (ServerAccountSettings).
        // We keep this reflection helper localized so core interfaces stay clean.
        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null || !prop.CanWrite) return;
        prop.SetValue(target, value);
    }
}


