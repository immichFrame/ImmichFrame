using System.Net.Http.Json;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class CustomWidgetService : ICustomWidgetService
{
    private readonly IGeneralSettings _settings;
    private readonly ILogger<CustomWidgetService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public CustomWidgetService(IGeneralSettings settings, ILogger<CustomWidgetService> logger, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<CustomWidgetData>> GetCustomWidgetData()
    {
        var results = new List<CustomWidgetData>();

        foreach (var source in _settings.CustomWidgetSources)
        {
            if (string.IsNullOrWhiteSpace(source.Url))
                continue;

            try
            {
                var cacheKey = $"customwidget_{source.Url}";
                var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(source.RefreshMinutes);
                    return await FetchWidgetData(source.Url);
                });

                if (data != null)
                    results.Add(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch custom widget data from '{Url}'", source.Url);
            }
        }

        return results;
    }

    private async Task<CustomWidgetData?> FetchWidgetData(string url)
    {
        var client = _httpClientFactory.CreateClient();

        try
        {
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Custom widget source '{Url}' returned status {StatusCode}", url, response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<CustomWidgetData>();
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching custom widget data from '{Url}'", url);
            return null;
        }
    }
}
