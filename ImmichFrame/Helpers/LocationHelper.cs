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
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                countries_info.Add(new RegionInfo(culture.TextInfo.CultureName));
            }
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

        public static string GetLocationString(ImmichFrame.Models.ExifResponseDto exifInfo)
        {
            string country = GetCountryCode(exifInfo.Country);
            string state = exifInfo.State?.Split(", ").Last() ?? string.Empty;

            var locationData = new[] {
                    exifInfo.City,
                    state,
                    country
                }.Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join(", ", locationData);
        }
    }
}
