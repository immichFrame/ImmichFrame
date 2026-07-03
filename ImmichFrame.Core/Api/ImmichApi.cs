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

        /// <summary>
        /// Searches memories with a properly formatted ISO 8601 datetime (including timezone offset).
        /// The NSwag-generated SearchMemoriesAsync uses the "s" format for DateTimeOffset, which omits
        /// the timezone offset. Immich v3 requires full ISO 8601 with timezone, so this method manually
        /// constructs the request with proper datetime formatting.
        /// </summary>
        public virtual async Task<ICollection<MemoryResponseDto>> SearchMemoriesWithProperDateAsync(
            DateTimeOffset searchDate,
            bool? isSaved = null,
            bool? isTrashed = null,
            MemorySearchOrder? order = null,
            int? size = null,
            MemoryType? type = null,
            CancellationToken cancellationToken = default)
        {
            var urlBuilder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder.Append(_baseUrl);
            urlBuilder.Append("memories?");

            // Format with full ISO 8601 including timezone offset
            urlBuilder.Append("for=");
            urlBuilder.Append(Uri.EscapeDataString(searchDate.ToString("yyyy-MM-ddTHH:mm:sszzz")));

            if (isSaved.HasValue)
            {
                urlBuilder.Append("&isSaved=");
                urlBuilder.Append(Uri.EscapeDataString(isSaved.Value.ToString().ToLowerInvariant()));
            }

            if (isTrashed.HasValue)
            {
                urlBuilder.Append("&isTrashed=");
                urlBuilder.Append(Uri.EscapeDataString(isTrashed.Value.ToString().ToLowerInvariant()));
            }

            if (order.HasValue)
            {
                urlBuilder.Append("&order=");
                var orderStr = Newtonsoft.Json.JsonConvert.SerializeObject(order.Value, JsonSerializerSettings).Trim('"');
                urlBuilder.Append(Uri.EscapeDataString(orderStr));
            }

            if (size.HasValue)
            {
                urlBuilder.Append("&size=");
                urlBuilder.Append(Uri.EscapeDataString(size.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            if (type.HasValue)
            {
                urlBuilder.Append("&type=");
                var typeStr = Newtonsoft.Json.JsonConvert.SerializeObject(type.Value, JsonSerializerSettings).Trim('"');
                urlBuilder.Append(Uri.EscapeDataString(typeStr));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.ToString());
            request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var headers = new Dictionary<string, IEnumerable<string>>();
            foreach (var item in response.Headers)
                headers[item.Key] = item.Value;
            if (response.Content?.Headers != null)
                foreach (var item in response.Content.Headers)
                    headers[item.Key] = item.Value;

            var status = (int)response.StatusCode;
            if (status == 200)
            {
                var json = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<MemoryResponseDto>>(json, JsonSerializerSettings);
                return result ?? new List<MemoryResponseDto>();
            }

            var error = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.Dispose();
            throw new ApiException($"Unexpected status code ({status}).", status, error, headers, null);
        }
    }
}
