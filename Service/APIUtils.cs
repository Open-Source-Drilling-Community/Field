using System;
using System.Net.Http;

namespace NORCE.Drilling.Field.Service
{
    public static class APIUtils
    {
        // API parameters
        public static readonly string HostNameCartographicProjection = Configuration.CartographicProjectionHostURL!;
        public static readonly string HostBasePathCartographicProjection = "CartographicProjection/api/";
        public static readonly HttpClient HttpClientCartographicProjection = SetHttpClient(HostNameCartographicProjection, HostBasePathCartographicProjection);
        public static readonly ModelShared.Client ClientCartographicProjection = new ModelShared.Client(HttpClientCartographicProjection.BaseAddress!.ToString(), HttpClientCartographicProjection);

        // API utility methods
        public static HttpClient SetHttpClient(string host, string microServiceUri)
        {
            HttpClient httpClient = new()
            {
                BaseAddress = new Uri(host + microServiceUri)
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
    }
}
