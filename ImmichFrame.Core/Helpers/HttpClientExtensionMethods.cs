namespace ImmichFrame.Core.Helpers
{
    public static class HttpClientExtensionMethods
    {
        public static void UseApiKey(this HttpClient client, string apiKey)
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
        }
    }
}
