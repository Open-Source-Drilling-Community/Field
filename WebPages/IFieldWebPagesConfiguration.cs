using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebPages;

public interface IFieldWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    ITrajectoryHostURL,
    ICartographicProjectionHostURL,
    IUnitConversionHostURL
{
    string? VerticalDatumHostURL { get; set; }
}
