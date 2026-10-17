using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OSDC.Drilling.Field.ModelShared;

/// <summary>Collects bounded discovery pages. Reference projections are for display/context only;
/// fetch a selected full record before editing or accessing engineering collections.</summary>
public static class DiscoveryClientExtensions
{
    private static Guid? DiscoveryIdentifier(object value)
    {
        object metadata = value.GetType().GetProperty("MetaInfo")?.GetValue(value);
        return (metadata?.GetType().GetProperty("ID")?.GetValue(metadata) ?? value.GetType().GetProperty("ID")?.GetValue(value)) as Guid?;
    }
}
