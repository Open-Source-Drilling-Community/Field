using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.Field.WebPages;

public interface IFieldWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    ITrajectoryHostURL,
    IUnitConversionHostURL
{
    string? EarthCartographicProjectionHostURL { get; set; }
    string? VerticalDatumHostURL { get; set; }
}
