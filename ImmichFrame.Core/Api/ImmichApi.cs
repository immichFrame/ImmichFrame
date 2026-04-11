namespace ImmichFrame.Core.Api
{
    public partial class ImmichApi
    {
        public ImmichApi(string url, HttpClient httpClient)
        {
            BaseUrl = url + "/api";
            _httpClient = httpClient;
            _settings = new Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);            
        }

        public async Task<FileResponse> PlayAssetVideoWithRangeAsync(Guid id, string rangeHeader, CancellationToken cancellationToken = default)
        {
            var urlBuilder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder.Append(_baseUrl);
            urlBuilder.Append("assets/");
            urlBuilder.Append(Uri.EscapeDataString(id.ToString("D", System.Globalization.CultureInfo.InvariantCulture)));
            urlBuilder.Append("/video/playback");

            using var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
            request.Headers.TryAddWithoutValidation("Range", rangeHeader);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var headers = new Dictionary<string, IEnumerable<string>>();
            foreach (var item in response.Headers)
                headers[item.Key] = item.Value;
            if (response.Content?.Headers != null)
                foreach (var item in response.Content.Headers)
                    headers[item.Key] = item.Value;

            var status = (int)response.StatusCode;
            if (status == 200 || status == 206)
            {
                var stream = response.Content == null ? Stream.Null : await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return new FileResponse(status, headers, stream, null, response);
            }

            var error = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.Dispose();
            throw new ApiException($"Unexpected status code ({status}).", status, error, headers, null);
        }
        partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, System.Text.StringBuilder urlBuilder)
        {
            var url = urlBuilder.ToString();

            // ensure every call to the albums endpoint includes shared content
            if (url.Contains("/albums/") && !url.Contains("shared=true"))
            {
                string separator = url.Contains("?") ? "&" : "?";
                urlBuilder.Append($"{separator}shared=true");
            }
        }
    }
}
