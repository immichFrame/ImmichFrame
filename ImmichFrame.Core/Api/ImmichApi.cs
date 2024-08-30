namespace ImmichFrame.Core.Api
{
    public partial class ImmichApi
    {
        public ImmichApi(string url, System.Net.Http.HttpClient httpClient)
        {
            _baseUrl = url+_baseUrl;
            _httpClient = httpClient;
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }
    }
}
