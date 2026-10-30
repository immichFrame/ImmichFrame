namespace ImmichFrame.WebApi.Helpers
{
    /// <summary>The client identifier a frame sends, made safe to log and to key per-client state by.</summary>
    public static class ClientIdentifier
    {
        /// <summary>The longest client identifier kept; it names the client's shuffle deck.</summary>
        public const int MaxLength = 64;

        public static string Sanitize(string? clientIdentifier)
        {
            var sanitized = (clientIdentifier ?? string.Empty).SanitizeString();
            return sanitized.Length > MaxLength ? sanitized[..MaxLength] : sanitized;
        }
    }
}
