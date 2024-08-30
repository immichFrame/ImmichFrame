using ImmichFrame.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ImmichFrame.Helpers
{
    public class LocationHelper
    {
        static List<RegionInfo> countries_info = new List<RegionInfo>();

        static LocationHelper()
        {
            countries_info = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(x => new RegionInfo(x.Name)).ToList();
        }

        public static string GetCountryCode(string country)
        {
            string target_country = country;
            if (country == "United States of America")
            {
                target_country = "United States";
            }
            RegionInfo? country_info = countries_info.Where(info => info.EnglishName == target_country).FirstOrDefault();
            return country_info?.ThreeLetterISORegionName ?? country;
        }

        public static string GetLocationString(ImmichFrame.Core.Api.ExifResponseDto exifInfo)
        {
            var locationParts = Settings.CurrentSettings.ImageLocationFormat?.Split(',') ?? Array.Empty<string>();

            var city = locationParts.Length >= 1 ? exifInfo.City : string.Empty;
            var state = locationParts.Length >= 2 ? (exifInfo.State?.Split(", ").Last() ?? string.Empty) : string.Empty;
            var country = locationParts.Length >= 3 ? GetCountryCode(exifInfo.Country) : string.Empty;

            return string.Join(", ", new[] { city, state, country }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
