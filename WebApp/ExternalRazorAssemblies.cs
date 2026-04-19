using System.Reflection;

namespace NORCE.Drilling.Field.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(NORCE.Drilling.Field.WebPages.Field).Assembly,
        typeof(NORCE.Drilling.CartographicProjection.WebPages.CartographicProjection).Assembly,
        typeof(NORCE.Drilling.GeodeticDatum.WebPages.GeodeticDatumMain).Assembly,
    ];
}
