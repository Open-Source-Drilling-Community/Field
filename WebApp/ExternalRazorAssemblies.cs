using System.Reflection;

namespace OSDC.Drilling.Field.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
        typeof(OSDC.Drilling.Cluster.WebPages.ClusterMain).Assembly,
    ];
}
