using OSDC.DotnetLibraries.Drilling.WebAppUtils;
using FieldModelShared = NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.WebPages;

public class FieldAPIUtils : APIUtils, IFieldAPIUtils
{
    private static readonly TimeSpan ProjectionDefinitionCacheLifetime = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim projectionDefinitionCacheLock = new(1, 1);
    private IReadOnlyList<FieldModelShared.ProjectionDefinitionSummary> projectionDefinitionCache = [];
    private DateTimeOffset projectionDefinitionCacheExpiresUtc = DateTimeOffset.MinValue;

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

        HostNameEarthCartographicProjection = Require(configuration.EarthCartographicProjectionHostURL, nameof(configuration.EarthCartographicProjectionHostURL));
        HttpClientEarthCartographicProjection = SetHttpClient(HostNameEarthCartographicProjection, HostBasePathEarthCartographicProjection);
        ClientEarthCartographicProjection = new FieldModelShared.Client(HttpClientEarthCartographicProjection.BaseAddress!.ToString(), HttpClientEarthCartographicProjection);

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

    public string HostNameEarthCartographicProjection { get; }
    public string HostBasePathEarthCartographicProjection { get; } = "EarthCartographicProjection/api/";
    public HttpClient HttpClientEarthCartographicProjection { get; }
    public FieldModelShared.Client ClientEarthCartographicProjection { get; }

    public async Task<IReadOnlyList<FieldModelShared.ProjectionDefinitionSummary>> GetProjectionDefinitionSummariesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FieldModelShared.ProjectionDefinitionSummary> current = projectionDefinitionCache;
        if (current.Count > 0 && DateTimeOffset.UtcNow < projectionDefinitionCacheExpiresUtc)
        {
            return current;
        }

        await projectionDefinitionCacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = projectionDefinitionCache;
            if (current.Count > 0 && DateTimeOffset.UtcNow < projectionDefinitionCacheExpiresUtc)
            {
                return current;
            }

            try
            {
                ICollection<FieldModelShared.ProjectionDefinitionSummary> summaries =
                    await ClientEarthCartographicProjection.SummariesAsync(false, cancellationToken).ConfigureAwait(false);
                projectionDefinitionCache = summaries
                    .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                projectionDefinitionCacheExpiresUtc = DateTimeOffset.UtcNow.Add(ProjectionDefinitionCacheLifetime);
            }
            catch (Exception exception) when (current.Count > 0 && exception is not OperationCanceledException)
            {
                // A stale catalog still permits editing during a transient dependency failure.
                // Leave it expired so the next request attempts to refresh it again.
                projectionDefinitionCacheExpiresUtc = DateTimeOffset.UtcNow;
            }

            return projectionDefinitionCache;
        }
        finally
        {
            projectionDefinitionCacheLock.Release();
        }
    }

    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";

    public string HostNameVerticalDatum { get; }
    public string HostBasePathVerticalDatum { get; } = "VerticalDatum/api/";
    public HttpClient HttpClientVerticalDatum { get; }
    public FieldModelShared.Client ClientVerticalDatum { get; }
}
