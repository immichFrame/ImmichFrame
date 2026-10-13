namespace ImmichFrame.Core.Helpers;

/// <summary>
/// Maps image content types to file extensions and back, for the served file name and the
/// on-disk cache. Originals can be any format Immich accepts, not just JPEG and WebP.
/// </summary>
public static class ImageContentTypes
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpeg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["image/gif"] = "gif",
        ["image/avif"] = "avif",
        ["image/heic"] = "heic",
        ["image/heif"] = "heif",
        ["image/jxl"] = "jxl",
        ["image/x-adobe-dng"] = "dng",
    };

    private static readonly Dictionary<string, string> ContentTypes =
        Extensions.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    // Same list as Immich's isWebSupportedImage: the originals Immich itself serves as "fullsize".
    private static readonly HashSet<string> WebSupported = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/avif"
    };

    public static string ToExtension(string? contentType)
    {
        var mediaType = MediaType(contentType);
        if (Extensions.TryGetValue(mediaType, out var extension))
            return extension;

        // Unknown (e.g. image/x-nikon-nef): derive something file-name safe from the subtype.
        var subtype = mediaType.Contains('/') ? mediaType[(mediaType.IndexOf('/') + 1)..] : mediaType;
        if (subtype.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
            subtype = subtype[2..];
        var safe = new string(subtype.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.').ToArray());
        return string.IsNullOrEmpty(safe) ? "bin" : safe.ToLowerInvariant();
    }

    public static string FromExtension(string extension) =>
        ContentTypes.TryGetValue(extension.TrimStart('.'), out var contentType)
            ? contentType
            : "application/octet-stream";

    public static bool IsWebSupported(string? contentType) => WebSupported.Contains(MediaType(contentType));

    // Drops parameters such as "; charset=...".
    private static string MediaType(string? contentType) =>
        (contentType ?? string.Empty).Split(';')[0].Trim();
}
