namespace ImmichFrame.Core.Models;

/// <summary>
/// Which rendition of a photo the server fetches from Immich (the <c>ImageSize</c> setting).
/// </summary>
public enum ImageSize
{
    /// <summary>Immich's preview (1440 px on the short side by default, JPEG or WebP).</summary>
    Preview,

    /// <summary>
    /// The original when a browser can display it, otherwise Immich's full-size rendition,
    /// otherwise the preview. Mirrors what Immich itself serves as <c>size=fullsize</c>.
    /// </summary>
    Fullsize,

    /// <summary>The original file, whatever its format (HEIC and RAW included).</summary>
    Original
}

public static class ImageSizeParser
{
    /// <summary>
    /// Case-insensitive. An unknown or empty value falls back to <see cref="ImageSize.Preview"/>
    /// instead of failing validation, like <c>Layout</c> and <c>Style</c>.
    /// </summary>
    public static ImageSize Parse(string? value) =>
        Enum.GetValues<ImageSize>().FirstOrDefault(
            size => string.Equals(size.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase),
            ImageSize.Preview);
}
