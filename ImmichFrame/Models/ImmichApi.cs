using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmichFrame.Models
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
