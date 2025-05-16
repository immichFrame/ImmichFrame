
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
}

