using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.Service;

public interface IEarthCartographicProjectionClient
{
    Task<ProjectionDefinition> GetDefinitionAsync(Guid id, CancellationToken cancellationToken);
    Task<ForwardProjectionResponse> ForwardAsync(ForwardProjectionRequest request, CancellationToken cancellationToken);
    Task<InverseProjectionResponse> InverseAsync(InverseProjectionRequest request, CancellationToken cancellationToken);
}

public interface IEarthGeodesyClient
{
    Task<ResolveTransformationPathsResponse> ResolvePathsAsync(ResolveTransformationPathsRequest request, CancellationToken cancellationToken);
    Task<TransformCoordinatesResponse> TransformAsync(TransformCoordinatesRequest request, CancellationToken cancellationToken);
}

public sealed class EarthCartographicProjectionClient : IEarthCartographicProjectionClient
{
    private readonly Client client;

    public EarthCartographicProjectionClient(HttpClient httpClient, IConfiguration configuration)
    {
        string host = Require(configuration["EarthCartographicProjectionHostURL"], "EarthCartographicProjectionHostURL");
        httpClient.BaseAddress = new Uri(new Uri(host.TrimEnd('/') + "/"), "EarthCartographicProjection/api/");
        client = new Client(httpClient.BaseAddress.ToString(), httpClient);
    }

    public Task<ProjectionDefinition> GetDefinitionAsync(Guid id, CancellationToken cancellationToken) =>
        client.ProjectionDefinitionGETAsync(id, cancellationToken);

    public Task<ForwardProjectionResponse> ForwardAsync(ForwardProjectionRequest request, CancellationToken cancellationToken) =>
        client.ForwardAsync(request, cancellationToken);

    public Task<InverseProjectionResponse> InverseAsync(InverseProjectionRequest request, CancellationToken cancellationToken) =>
        client.InverseAsync(request, cancellationToken);

    private static string Require(string? value, string key) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Configuration value '{key}' is required.") : value;
}

public sealed class EarthGeodesyClient : IEarthGeodesyClient
{
    private readonly Client client;

    public EarthGeodesyClient(HttpClient httpClient, IConfiguration configuration)
    {
        string host = Require(configuration["EarthGeodesyHostURL"], "EarthGeodesyHostURL");
        httpClient.BaseAddress = new Uri(new Uri(host.TrimEnd('/') + "/"), "EarthGeodesy/api/");
        client = new Client(httpClient.BaseAddress.ToString(), httpClient);
    }

    public Task<ResolveTransformationPathsResponse> ResolvePathsAsync(ResolveTransformationPathsRequest request, CancellationToken cancellationToken) =>
        client.TransformationPathsAsync(request, cancellationToken);

    public Task<TransformCoordinatesResponse> TransformAsync(TransformCoordinatesRequest request, CancellationToken cancellationToken) =>
        client.TransformAsync(request, cancellationToken);

    private static string Require(string? value, string key) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Configuration value '{key}' is required.") : value;
}
