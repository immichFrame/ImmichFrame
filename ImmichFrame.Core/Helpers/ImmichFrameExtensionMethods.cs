
using ImmichFrame.Core.Api;
using Microsoft.Extensions.Logging;

public static class ImmichFrameExtensionMethods
{
    public static string SanitizeString(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string removeChars = "'\"?&^$#@!()+-,:;<>’\'-_*/\\|`~{}[]";

        input = input.Trim();

        foreach (char c in removeChars)
        {
            input = input.Replace(c.ToString(), string.Empty);
        }

        input = input.Replace(Environment.NewLine, string.Empty);
        input = input.Replace("\r", string.Empty);
        input = input.Replace("\n", string.Empty);

        return input;
    }

    public async static Task<AssetResponseDto> LoadAdditionalAssetInfo(this AssetResponseDto asset, ImmichApi api, ILogger logger)
    {
        logger.LogTrace($"Loading additional info for asset {asset.Id}");
        var additional = await api.GetAssetInfoAsync(Guid.Parse(asset.Id), null);

        return additional;
    }
}

