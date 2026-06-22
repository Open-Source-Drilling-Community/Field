using NORCE.Drilling.Field.WebPages;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebApp;

public class WebPagesHostConfiguration :
    IFieldWebPagesConfiguration,
    NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionWebPagesConfiguration,
    NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumWebPagesConfiguration,
    NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumWebPageConfiguration
{
    public string? FieldHostURL { get; set; } = string.Empty;
    public string? ClusterHostURL { get; set; } = string.Empty;
    public string? TrajectoryHostURL { get; set; } = string.Empty;
    public string? CartographicProjectionHostURL { get; set; } = string.Empty;
    public string? GeodeticDatumHostURL { get; set; } = string.Empty;
    public string? VerticalDatumHostURL { get; set; } = string.Empty;
    public string? UnitConversionHostURL { get; set; } = string.Empty;

    string NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumWebPageConfiguration.VerticalDatumHostURL => VerticalDatumHostURL ?? string.Empty;
}
