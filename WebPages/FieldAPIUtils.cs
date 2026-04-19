using OSDC.DotnetLibraries.Drilling.WebAppUtils;
using FieldModelShared = NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.WebPages;

public class FieldAPIUtils : APIUtils, IFieldAPIUtils
{
    public FieldAPIUtils(IFieldWebPagesConfiguration configuration)
    {
        HostNameField = Require(configuration.FieldHostURL, nameof(configuration.FieldHostURL));
        HttpClientField = SetHttpClient(HostNameField, HostBasePathField);
        ClientField = new FieldModelShared.Client(HttpClientField.BaseAddress!.ToString(), HttpClientField);

        HostNameTrajectory = Require(configuration.TrajectoryHostURL, nameof(configuration.TrajectoryHostURL));
        HttpClientTrajectory = SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
        ClientTrajectory = new FieldModelShared.Client(HttpClientTrajectory.BaseAddress!.ToString(), HttpClientTrajectory);

        HostNameCartographicProjection = Require(configuration.CartographicProjectionHostURL, nameof(configuration.CartographicProjectionHostURL));
        HttpClientCartographicProjection = SetHttpClient(HostNameCartographicProjection, HostBasePathCartographicProjection);
        ClientCartographicProjection = new FieldModelShared.Client(HttpClientCartographicProjection.BaseAddress!.ToString(), HttpClientCartographicProjection);

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
    public FieldModelShared.Client ClientField { get; }

    public string HostNameTrajectory { get; }
    public string HostBasePathTrajectory { get; } = "Trajectory/api/";
    public HttpClient HttpClientTrajectory { get; }
    public FieldModelShared.Client ClientTrajectory { get; }

    public string HostNameCartographicProjection { get; }
    public string HostBasePathCartographicProjection { get; } = "CartographicProjection/api/";
    public HttpClient HttpClientCartographicProjection { get; }
    public FieldModelShared.Client ClientCartographicProjection { get; }

    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";
}
