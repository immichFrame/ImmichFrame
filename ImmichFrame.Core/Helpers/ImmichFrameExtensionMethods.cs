using System.Text;

public static class ImmichFrameExtensionMethods
{
    /// <summary>Keeps only ASCII letters, digits, '.', '_' and '-'; everything else is dropped.</summary>
    public static string SanitizeString(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sanitized = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')
            {
                sanitized.Append(c);
            }
        }

        return sanitized.ToString();
    }
}
