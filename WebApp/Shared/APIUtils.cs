public static class APIUtils
{
    // API parameters
    public static readonly string HostNameField = NORCE.Drilling.Field.WebApp.Configuration.FieldHostURL!;
    public static readonly string HostBasePathField = "Field/api/";
    public static readonly HttpClient HttpClientField = APIUtils.SetHttpClient(HostNameField, HostBasePathField);
    public static readonly NORCE.Drilling.Field.ModelShared.Client ClientField = new NORCE.Drilling.Field.ModelShared.Client(APIUtils.HttpClientField.BaseAddress!.ToString(), APIUtils.HttpClientField);

    public static readonly string HostNameCartographicProjection = NORCE.Drilling.Field.WebApp.Configuration.CartographicProjectionHostURL!;
    public static readonly string HostBasePathCartographicProjection = "CartographicProjection/api/";
    public static readonly HttpClient HttpClientCartographicProjection = SetHttpClient(HostNameCartographicProjection, HostBasePathCartographicProjection);
    public static readonly NORCE.Drilling.Field.ModelShared.Client ClientCartographicProjection = new NORCE.Drilling.Field.ModelShared.Client(APIUtils.HttpClientCartographicProjection.BaseAddress!.ToString(), APIUtils.HttpClientCartographicProjection);

    public static readonly string HostNameUnitConversion = NORCE.Drilling.Field.WebApp.Configuration.UnitConversionHostURL!;
    public static readonly string HostBasePathUnitConversion = "UnitConversion/api/";

    // API utility methods
    public static HttpClient SetHttpClient(string host, string microServiceUri)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(host + microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }
}