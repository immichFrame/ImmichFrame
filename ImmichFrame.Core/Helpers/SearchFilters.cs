using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Helpers;

public static class SearchFilters
{
    public static NullableDateFilter NotTrashed() => new() { Eq = null };

    public static EnumFilterAssetVisibility Visibility(IAccountSettings settings)
    {
        var visibilities = new List<AssetVisibility> { AssetVisibility.Timeline };
        if (settings.ShowArchived)
        {
            visibilities.Add(AssetVisibility.Archive);
        }

        return new EnumFilterAssetVisibility { In = visibilities };
    }

    public static IdsFilter? ExcludedAlbums(IAccountSettings settings)
    {
        if (settings.ExcludedAlbums is not { Count: > 0 } excluded)
        {
            return null;
        }

        return new IdsFilter { None = excluded };
    }

    public static EnumFilterAssetType Types(IAccountSettings settings)
    {
        var types = new List<AssetTypeEnum> { AssetTypeEnum.IMAGE };
        if (settings.ShowVideos)
        {
            types.Add(AssetTypeEnum.VIDEO);
        }

        return new EnumFilterAssetType { In = types };
    }

    public static bool HasConfiguredSource(IAccountSettings settings) =>
        settings.ShowFavorites
        || settings.Albums is { Count: > 0 }
        || settings.People is { Count: > 0 }
        || settings.Tags is { Count: > 0 };

    public static SearchFilter ForAccount(IAccountSettings settings, IReadOnlyCollection<Guid> tagIds)
    {
        var filter = new SearchFilter
        {
            TrashedAt = NotTrashed(),
            Visibility = Visibility(settings),
            Type = Types(settings),
            AlbumIds = ExcludedAlbums(settings),
            TakenAt = TakenAt(settings),
            Rating = Rating(settings)
        };

        var branches = Branches(settings, tagIds);
        if (branches.Count > 0)
        {
            filter.Or = branches;
        }

        return filter;
    }

    public static bool HasMatchingBranch(IAccountSettings settings, IReadOnlyCollection<Guid> tagIds) =>
        Branches(settings, tagIds).Count > 0;

    private static List<SearchFilterBranch> Branches(IAccountSettings settings, IReadOnlyCollection<Guid> tagIds)
    {
        var branches = new List<SearchFilterBranch>();
        if (settings.ShowFavorites)
        {
            branches.Add(new SearchFilterBranch { IsFavorite = new BoolFilter { Eq = true } });
        }

        if (settings.Albums is { Count: > 0 } albums)
        {
            branches.Add(new SearchFilterBranch { AlbumIds = new IdsFilter { Any = albums } });
        }

        if (settings.People is { Count: > 0 } people)
        {
            branches.Add(new SearchFilterBranch { PersonIds = new IdsFilter { Any = people } });
        }

        if (tagIds.Count > 0)
        {
            branches.Add(new SearchFilterBranch { TagIds = new IdsFilter { Any = tagIds.ToList() } });
        }

        return branches;
    }

    private static DateFilter? TakenAt(IAccountSettings settings)
    {
        var takenAt = new DateFilter();
        var hasTakenFilter = false;
        if (settings.ImagesUntilDate is DateTime until)
        {
            takenAt.Lt = until;
            hasTakenFilter = true;
        }

        DateTime? from = settings.ImagesFromDate
            ?? (settings.ImagesFromDays is int days ? DateTime.Today.AddDays(-days) : null);
        if (from is DateTime after)
        {
            takenAt.Gt = after;
            hasTakenFilter = true;
        }

        return hasTakenFilter ? takenAt : null;
    }

    private static NumberFilterNullable? Rating(IAccountSettings settings) =>
        settings.Rating is int rating ? new NumberFilterNullable { Eq = rating } : null;
}
