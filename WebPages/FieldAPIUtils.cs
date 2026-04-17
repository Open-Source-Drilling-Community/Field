using NORCE.Drilling.Field.ModelShared;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebPages;

public class FieldAPIUtils : APIUtils, IFieldAPIUtils
{
    public FieldAPIUtils(IFieldWebPagesConfiguration configuration)
    {
        HostNameField = Require(configuration.FieldHostURL, nameof(configuration.FieldHostURL));
        HttpClientField = SetHttpClient(HostNameField, HostBasePathField);
        ClientField = new Client(HttpClientField.BaseAddress!.ToString(), HttpClientField);

        HostNameCartographicProjection = Require(configuration.CartographicProjectionHostURL, nameof(configuration.CartographicProjectionHostURL));
        HttpClientCartographicProjection = SetHttpClient(HostNameCartographicProjection, HostBasePathCartographicProjection);
        ClientCartographicProjection = new Client(HttpClientCartographicProjection.BaseAddress!.ToString(), HttpClientCartographicProjection);

        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));
    }

    private static string Require(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{propertyName}' must be assigned before WebPages is used.");
        }

        return value;
    }

    public string HostNameField { get; }
    public string HostBasePathField { get; } = "Field/api/";
    public HttpClient HttpClientField { get; }
    public Client ClientField { get; }

    public string HostNameCartographicProjection { get; }
    public string HostBasePathCartographicProjection { get; } = "CartographicProjection/api/";
    public HttpClient HttpClientCartographicProjection { get; }
    public Client ClientCartographicProjection { get; }

    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";
}
