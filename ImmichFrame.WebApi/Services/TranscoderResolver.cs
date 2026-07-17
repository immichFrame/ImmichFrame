using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ImmichFrame.WebApi.Services;

/// <summary>
/// Small client for the optional immichframe-transcoder sidecar.  Keeping this
/// outside Core makes the feature opt-in: installations without the sidecar
/// retain the original Immich video proxy behaviour.
/// </summary>
public interface ITranscoderResolver
{
    Task<TranscoderResolution> ResolveAsync(Guid assetId, string checksum, CancellationToken cancellationToken);
}

public enum TranscoderResolutionKind { Disabled, Direct, Ready, Pending }

public sealed record TranscoderResolution(TranscoderResolutionKind Kind, string? FilePath = null);

public sealed class TranscoderResolver : ITranscoderResolver
{
    private readonly HttpClient _httpClient;
    private readonly string? _mediaPath;
    private readonly string _profile;
    private readonly ILogger<TranscoderResolver> _logger;

    public TranscoderResolver(IHttpClientFactory httpClientFactory, ILogger<TranscoderResolver> logger)
    {
        _logger = logger;
        var url = Environment.GetEnvironmentVariable("TRANSCODER_URL");
        _mediaPath = Environment.GetEnvironmentVariable("TRANSCODER_MEDIA_PATH");
        _profile = Environment.GetEnvironmentVariable("TRANSCODER_PLAYBACK_PROFILE") ?? "720";
        _httpClient = httpClientFactory.CreateClient("Transcoder");
        if (!string.IsNullOrWhiteSpace(url))
        {
            _httpClient.BaseAddress = new Uri(url.TrimEnd('/') + "/");
            _logger.LogInformation("Video transcoder enabled at {TranscoderUrl}; playback profile {PlaybackProfile}", _httpClient.BaseAddress, _profile);
        }
        else
        {
            _logger.LogInformation("Video transcoder is disabled: TRANSCODER_URL is not configured");
        }
    }

    public async Task<TranscoderResolution> ResolveAsync(Guid assetId, string checksum, CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress == null || string.IsNullOrWhiteSpace(_mediaPath))
        {
            _logger.LogInformation("Video {AssetId} bypasses transcoder because it is not fully configured", assetId);
            return new(TranscoderResolutionKind.Disabled);
        }

        try
        {
            _logger.LogInformation("Resolving video {AssetId} with transcoder; profile {PlaybackProfile}", assetId, _profile);
            using var response = await _httpClient.PostAsJsonAsync("v1/resolve",
                new ResolveRequest(assetId, checksum, _profile), cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ResolveResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("Transcoder returned status {TranscoderStatus} and codec {Codec} for video {AssetId}", result?.Status ?? "(empty)", result?.Codec ?? "unknown", assetId);

            return result?.Status switch
            {
                "direct" => new(TranscoderResolutionKind.Direct),
                "ready" when File.Exists(Path.Combine(_mediaPath, _profile, assetId + ".mp4"))
                    => new(TranscoderResolutionKind.Ready, Path.Combine(_mediaPath, _profile, assetId + ".mp4")),
                _ => new(TranscoderResolutionKind.Pending)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // A down sidecar must never interrupt existing playback; use Immich as before.
            _logger.LogWarning(ex, "Unable to resolve transcoding state for video {AssetId}; using Immich playback", assetId);
            return new(TranscoderResolutionKind.Disabled);
        }
    }

    private sealed record ResolveRequest(Guid AssetId, string Checksum, string PlaybackProfile);
    private sealed class ResolveResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("codec")]
        public string? Codec { get; init; }
    }
}
