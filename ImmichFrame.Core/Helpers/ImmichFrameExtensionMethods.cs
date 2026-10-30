using System.Text;

public static class ImmichFrameExtensionMethods
{
    /// <summary>
    /// Keeps letters and digits in any script, spaces and '.'; drops everything else
    /// (control and format characters, quotes, other punctuation, '-' and '_'), then trims.
    /// For ordinary names this gives the same result as the earlier blocklist.
    /// </summary>
    public static string SanitizeString(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sanitized = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c is ' ' or '.')
            {
                sanitized.Append(c);
            }
        }

        return sanitized.ToString().Trim();
    }
}
