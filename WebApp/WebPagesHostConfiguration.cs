using OSDC.Drilling.Field.WebPages;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.Field.WebApp;

public class WebPagesHostConfiguration :
    IFieldWebPagesConfiguration,
    OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration,
    OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration,
    OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration,
    OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldWebPagesConfiguration,
    OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration
{
    public string? FieldHostURL { get; set; } = string.Empty;
    public string? ClusterHostURL { get; set; } = string.Empty;
    public string? TrajectoryHostURL { get; set; } = string.Empty;
    public string? EarthCartographicProjectionHostURL { get; set; } = string.Empty;
    public string? EarthGeodesyHostURL { get; set; } = string.Empty;
    public string EarthGravityHostURL { get; set; } = string.Empty;
    public string EarthMagneticFieldHostURL { get; set; } = string.Empty;
    public string? EarthVerticalDatumHostURL { get; set; } = string.Empty;
    public string? UnitConversionHostURL { get; set; } = string.Empty;

    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.ServiceUrl => EarthCartographicProjectionHostURL ?? string.Empty;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.EarthGeodesyUrl => EarthGeodesyHostURL ?? string.Empty;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.UnitConversionUrl => UnitConversionHostURL ?? string.Empty;
    string OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration.EarthGeodesyHostURL => EarthGeodesyHostURL ?? string.Empty;
    string OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration.EarthVerticalDatumHostURL => EarthVerticalDatumHostURL ?? string.Empty;
}
