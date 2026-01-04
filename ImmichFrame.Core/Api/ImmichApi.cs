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
    }
}
