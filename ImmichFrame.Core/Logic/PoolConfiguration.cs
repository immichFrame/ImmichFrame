using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Logic;

public class PoolConfiguration
{
    public ImmichApi ImmichApi { get; }
    public ApiCache ApiCache { get; }
    
    public PoolConfiguration(IAccountSettings accountSettings, IGeneralSettings generalSettings, IHttpClientFactory httpClientFactory)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.UseApiKey(accountSettings.ApiKey);
        
        ImmichApi = new ImmichApi(accountSettings.ImmichServerUrl, httpClient);
        ApiCache = new ApiCache(TimeSpan.FromHours(generalSettings.RefreshAlbumPeopleInterval));
    }
}