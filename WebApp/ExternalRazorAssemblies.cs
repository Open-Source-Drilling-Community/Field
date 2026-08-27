using System.Reflection;

namespace OSDC.Drilling.Field.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
        typeof(NORCE.Drilling.GeodeticDatum.WebPages.GeodeticDatumMain).Assembly,
        typeof(NORCE.Drilling.VerticalDatum.WebPage.VerticalDatumConversionMain).Assembly,
    ];
}
