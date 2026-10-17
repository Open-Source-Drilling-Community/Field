using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace OSDC.Drilling.Field.ModelShared;
public static class ProjectionDiscoveryClientExtensions
{
    public static async Task<List<ProjectionDefinitionSummary>> ListProjectionDefinitionSummariesAsync(this Client client, bool includeDeprecated = false, CancellationToken cancellationToken = default)
    {
        var summaries = new List<ProjectionDefinitionSummary>();
        int offset = 0;
        while (true)
        {
            var page = await client.SummariesAsync(includeDeprecated: includeDeprecated, offset: offset, limit: 200, cancellationToken: cancellationToken);
            summaries.AddRange(page.Items);
            if (page.NextOffset is not int next) return summaries;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }
}
