using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers
{
    public static class AssetExtensionMethods
    {
        public static bool IsSupportedAsset(this AssetResponseDto asset)
        {
            return asset.Type == AssetTypeEnum.IMAGE || asset.Type == AssetTypeEnum.VIDEO;
        }

        public static async Task<IEnumerable<AssetResponseDto>> ApplyAccountFilters(this Task<IEnumerable<AssetResponseDto>> unfilteredAssets, IAccountSettings accountSettings, IEnumerable<AssetResponseDto> excludedAlbumAssets)
        {
            return ApplyAccountFilters(await unfilteredAssets, accountSettings, excludedAlbumAssets);
        }

        public static IEnumerable<AssetResponseDto> ApplyAccountFilters(this IEnumerable<AssetResponseDto> unfilteredAssets, IAccountSettings accountSettings, IEnumerable<AssetResponseDto> excludedAlbumAssets)
        {
            // Display supported media types
            var assets = unfilteredAssets.Where(asset => asset.IsSupportedAsset());

            if (!accountSettings.ShowVideos)
                assets = assets.Where(x => x.Type == AssetTypeEnum.IMAGE);

            if (!accountSettings.ShowArchived)
                assets = assets.Where(x => x.IsArchived == false);

            var takenBefore = accountSettings.ImagesUntilDate.HasValue ? accountSettings.ImagesUntilDate : null;
            if (takenBefore.HasValue)
            {
                assets = assets.Where(x => x.ExifInfo?.DateTimeOriginal != null && x.ExifInfo.DateTimeOriginal <= takenBefore);
            }

            var takenAfter = accountSettings.ImagesFromDate.HasValue ? accountSettings.ImagesFromDate : accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null;
            if (takenAfter.HasValue)
            {
                assets = assets.Where(x => x.ExifInfo?.DateTimeOriginal != null && x.ExifInfo.DateTimeOriginal >= takenAfter);
            }

            if (accountSettings.Rating is int rating)
            {
                assets = assets.Where(x => x.ExifInfo?.Rating == rating);
            }

            assets = assets.WhereExcludes(excludedAlbumAssets, t => t.Id);

            return assets;
        }
    }
}
