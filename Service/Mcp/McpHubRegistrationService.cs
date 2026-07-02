using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NORCE.Drilling.Field.Service.Managers;

namespace NORCE.Drilling.Field.Service.Mcp;

public sealed class McpHubRegistrationService : BackgroundService
{
    public static readonly Guid ServiceTypeId = Guid.Parse("e4e17db0-143c-45d7-8244-8756d93b5ff0");

    private const string InstanceIdFileName = "field-mcp-hub-instance-id.txt";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpHubRegistrationService> _logger;
    private readonly IOptionsMonitor<McpHubOptions> _options;

    private Guid? _registeredInstanceId;

    public McpHubRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<McpHubRegistrationService> logger,
        IOptionsMonitor<McpHubOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (!options.Enabled)
        {
            _logger.LogInformation("MCP hub registration is disabled.");
            return;
        }
        if (string.IsNullOrWhiteSpace(options.HubBaseUrl))
        {
            _logger.LogInformation("MCP hub registration skipped because McpHub:HubBaseUrl is not configured.");
            return;
        }
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            _logger.LogWarning("MCP hub registration skipped because McpHub:PublicBaseUrl is not configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan retryInterval = GetRetryInterval(options);
            try
            {
                options = _options.CurrentValue;
                retryInterval = GetRetryInterval(options);
                if (!options.Enabled || string.IsNullOrWhiteSpace(options.HubBaseUrl) || string.IsNullOrWhiteSpace(options.PublicBaseUrl))
                {
                    _logger.LogInformation("MCP hub registration stopped because configuration is no longer complete.");
                    return;
                }

                Guid instanceId = ResolveInstanceId(options);
                McpHubRegistration registration = CreateRegistration(options, instanceId);
                using HttpClient client = _httpClientFactory.CreateClient(nameof(McpHubRegistrationService));
                Uri collectionUri = CreateCollectionUri(options);
                Uri itemUri = new(collectionUri, instanceId.ToString());

                HttpResponseMessage response = await client.PutAsJsonAsync(itemUri, registration, stoppingToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    response.Dispose();
                    response = await client.PostAsJsonAsync(collectionUri, registration, stoppingToken).ConfigureAwait(false);
                }

                if (response.IsSuccessStatusCode)
                {
                    _registeredInstanceId = instanceId;
                    _logger.LogInformation("Registered Field MCP endpoint on MCP hub at {HubUri}", collectionUri);
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogWarning(
                        "MCP hub registration failed with status {StatusCode}: {ResponseContent}. Retrying later.",
                        response.StatusCode,
                        responseContent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP hub registration attempt failed. Retrying later.");
            }

            try
            {
                await Task.Delay(retryInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        McpHubOptions options = _options.CurrentValue;
        if (options.UnregisterOnShutdown && _registeredInstanceId.HasValue && !string.IsNullOrWhiteSpace(options.HubBaseUrl))
        {
            try
            {
                using HttpClient client = _httpClientFactory.CreateClient(nameof(McpHubRegistrationService));
                Uri collectionUri = CreateCollectionUri(options);
                Uri itemUri = new(collectionUri, _registeredInstanceId.Value.ToString());
                HttpResponseMessage response = await client.DeleteAsync(itemUri, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("MCP hub unregister failed with status {StatusCode}", response.StatusCode);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP hub unregister failed during shutdown.");
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static McpHubRegistration CreateRegistration(McpHubOptions options, Guid instanceId)
    {
        string publicBaseUrl = options.PublicBaseUrl!.TrimEnd('/');
        string mcpHttpUrl = $"{publicBaseUrl}/Field/api/mcp";
        string mcpWebSocketUrl = ToWebSocketUrl($"{publicBaseUrl}/Field/api/mcp/ws");

        return new McpHubRegistration(
            ServiceTypeId,
            instanceId,
            string.IsNullOrWhiteSpace(options.ServiceName) ? "Field" : options.ServiceName,
            mcpHttpUrl,
            mcpWebSocketUrl,
            DateTimeOffset.UtcNow);
    }

    private static Uri CreateCollectionUri(McpHubOptions options)
    {
        string hubBaseUrl = options.HubBaseUrl!.TrimEnd('/') + "/";
        string endpoint = string.IsNullOrWhiteSpace(options.RegistrationEndpoint)
            ? "McpMicroservice"
            : options.RegistrationEndpoint.Trim('/');
        return new Uri(new Uri(hubBaseUrl), endpoint + "/");
    }

    private static TimeSpan GetRetryInterval(McpHubOptions options)
    {
        return TimeSpan.FromSeconds(Math.Max(1, options.RetryIntervalSeconds));
    }

    private static Guid ResolveInstanceId(McpHubOptions options)
    {
        if (Guid.TryParse(options.InstanceId, out Guid configuredId) && configuredId != Guid.Empty)
        {
            return configuredId;
        }

        Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);
        string instanceIdFile = Path.Combine(SqlConnectionManager.HOME_DIRECTORY, InstanceIdFileName);
        if (File.Exists(instanceIdFile) && Guid.TryParse(File.ReadAllText(instanceIdFile), out Guid persistedId) && persistedId != Guid.Empty)
        {
            return persistedId;
        }

        Guid generatedId = Guid.NewGuid();
        File.WriteAllText(instanceIdFile, generatedId.ToString());
        return generatedId;
    }

    private static string ToWebSocketUrl(string url)
    {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "wss://" + url["https://".Length..];
        }
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return "ws://" + url["http://".Length..];
        }
        return url;
    }

    private sealed record McpHubRegistration(
        Guid ServiceTypeId,
        Guid InstanceId,
        string Name,
        string McpHttpUrl,
        string McpWebSocketUrl,
        DateTimeOffset LastSeenUtc);
}
