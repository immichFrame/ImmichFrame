using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    internal static class HttpClientExtensionMethods
    {
        internal static void UseApiKey(this HttpClient client, string apiKey)
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
        }
    }
}
