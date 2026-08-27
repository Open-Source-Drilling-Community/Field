using OSDC.Drilling.Field.WebPages;
using FieldModelShared = OSDC.Drilling.Field.ModelShared;

namespace OSDC.Drilling.Field.WebApp;

/// <summary>
/// Warms the shared projection-summary cache without delaying WebApp readiness.
/// A failed warm-up is non-fatal; the Field page retries through the same cache.
/// </summary>
public sealed class ProjectionDefinitionCatalogWarmupService(
    IFieldAPIUtils api,
    ILogger<ProjectionDefinitionCatalogWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            IReadOnlyList<FieldModelShared.ProjectionDefinitionSummary> summaries =
                await api.GetProjectionDefinitionSummariesAsync(stoppingToken);
            logger.LogInformation("Warmed cartographic projection catalog with {Count} summaries", summaries.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to warm the cartographic projection catalog; it will be retried when the Field page is opened");
        }
    }
}
