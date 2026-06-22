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

        HostNameCluster = Require(configuration.ClusterHostURL, nameof(configuration.ClusterHostURL));
        HttpClientCluster = SetHttpClient(HostNameCluster, HostBasePathCluster);
        ClientCluster = new FieldModelShared.Client(HttpClientCluster.BaseAddress!.ToString(), HttpClientCluster);

        HostNameTrajectory = Require(configuration.TrajectoryHostURL, nameof(configuration.TrajectoryHostURL));
        HttpClientTrajectory = SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
        ClientTrajectory = new FieldModelShared.Client(HttpClientTrajectory.BaseAddress!.ToString(), HttpClientTrajectory);

        HostNameCartographicProjection = Require(configuration.CartographicProjectionHostURL, nameof(configuration.CartographicProjectionHostURL));
        HttpClientCartographicProjection = SetHttpClient(HostNameCartographicProjection, HostBasePathCartographicProjection);
        ClientCartographicProjection = new FieldModelShared.Client(HttpClientCartographicProjection.BaseAddress!.ToString(), HttpClientCartographicProjection);

        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));

        HostNameVerticalDatum = Require(configuration.VerticalDatumHostURL, nameof(configuration.VerticalDatumHostURL));
        HttpClientVerticalDatum = SetHttpClient(HostNameVerticalDatum, HostBasePathVerticalDatum);
        ClientVerticalDatum = new FieldModelShared.Client(HttpClientVerticalDatum.BaseAddress!.ToString(), HttpClientVerticalDatum);
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

    public string HostNameCluster { get; }
    public string HostBasePathCluster { get; } = "Cluster/api/";
    public HttpClient HttpClientCluster { get; }
    public FieldModelShared.Client ClientCluster { get; }

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

    public string HostNameVerticalDatum { get; }
    public string HostBasePathVerticalDatum { get; } = "VerticalDatum/api/";
    public HttpClient HttpClientVerticalDatum { get; }
    public FieldModelShared.Client ClientVerticalDatum { get; }
}
