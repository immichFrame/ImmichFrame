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

        // NSwag serializes DateTimeOffset query parameters with the "s" format, which drops the
        // timezone offset (e.g. "2026-07-03T14:30:00"). Newer Immich versions validate these
        // parameters as full ISO 8601 datetimes and reject any value without a timezone, causing a
        // 400 on the memories endpoints ("for" parameter). Re-append the offset before sending.
        // Only the "for" query parameter is affected; date filters for search go through the JSON
        // body, which Newtonsoft already serializes with an offset.
        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, System.Text.StringBuilder urlBuilder)
        {
            EnsureTimezoneOffset(urlBuilder, "for");
        }

        private static void EnsureTimezoneOffset(System.Text.StringBuilder urlBuilder, string parameterName)
        {
            var url = urlBuilder.ToString();
            var token = parameterName + "=";

            var idx = url.IndexOf("?" + token, StringComparison.Ordinal);
            if (idx < 0) idx = url.IndexOf("&" + token, StringComparison.Ordinal);
            if (idx < 0) return;

            var valueStart = idx + 1 + token.Length;
            var valueEnd = url.IndexOf('&', valueStart);
            if (valueEnd < 0) valueEnd = url.Length;

            var encodedValue = url.Substring(valueStart, valueEnd - valueStart);
            var rawValue = Uri.UnescapeDataString(encodedValue);

            // Only rewrite values that look like a datetime; leave anything else untouched.
            // The original offset is already lost at this point (stripped by "s"), so we re-infer it
            // with AssumeLocal, i.e. the machine's local offset. This is correct for the only caller
            // today (SearchMemoriesAsync with DateTimeOffset.Now). If a caller ever passes a value
            // with a different offset (e.g. UTC), it would be normalized to the local offset here.
            if (!DateTimeOffset.TryParse(rawValue, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeLocal, out var value))
                return;

            var fixedValue = Uri.EscapeDataString(
                value.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture));
            if (fixedValue == encodedValue) return;

            urlBuilder.Remove(valueStart, valueEnd - valueStart);
            urlBuilder.Insert(valueStart, fixedValue);
        }
    }
}
