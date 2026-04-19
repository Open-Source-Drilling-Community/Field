using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebPages;

public interface IFieldWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    IWellHostURL,
    IWellBoreHostURL,
    ITrajectoryHostURL,
    ICartographicProjectionHostURL,
    IUnitConversionHostURL
{
}
