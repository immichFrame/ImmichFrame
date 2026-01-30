using System.Net.Http.Headers;

namespace ImmichFrame.Core.Api
{
    public partial class ImmichApi
    {
        public ImmichApi(string url, System.Net.Http.HttpClient httpClient)
        {
            BaseUrl = url + "/api";
            _httpClient = httpClient;
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }

        public async Task<VideoStreamResponse> PlayAssetVideoWithRangeAsync(Guid id, string? rangeHeader, CancellationToken ct = default)
        {
            var requestUri = $"{BaseUrl}assets/{System.Uri.EscapeDataString(id.ToString())}/video/playback";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            if (!string.IsNullOrWhiteSpace(rangeHeader))
            {
                request.Headers.Range = RangeHeaderValue.Parse(rangeHeader);
            }

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            return new VideoStreamResponse(response);
        }
    }
}
