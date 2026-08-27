using FieldModelShared = OSDC.Drilling.Field.ModelShared;

namespace OSDC.Drilling.Field.WebPages;

public interface IFieldAPIUtils
{
    string HostNameField { get; }
    string HostBasePathField { get; }
    HttpClient HttpClientField { get; }
    FieldModelShared.Client ClientField { get; }

    string HostNameCluster { get; }
    string HostBasePathCluster { get; }
    HttpClient HttpClientCluster { get; }
    FieldModelShared.Client ClientCluster { get; }

    string HostNameTrajectory { get; }
    string HostBasePathTrajectory { get; }
    HttpClient HttpClientTrajectory { get; }
    FieldModelShared.Client ClientTrajectory { get; }

    string HostNameEarthCartographicProjection { get; }
    string HostBasePathEarthCartographicProjection { get; }
    HttpClient HttpClientEarthCartographicProjection { get; }
    FieldModelShared.Client ClientEarthCartographicProjection { get; }
    Task<IReadOnlyList<FieldModelShared.ProjectionDefinitionSummary>> GetProjectionDefinitionSummariesAsync(CancellationToken cancellationToken = default);

    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }

    string HostNameVerticalDatum { get; }
    string HostBasePathVerticalDatum { get; }
    HttpClient HttpClientVerticalDatum { get; }
    FieldModelShared.Client ClientVerticalDatum { get; }
}
