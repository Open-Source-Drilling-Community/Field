using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebPages;

public interface IFieldWebPagesConfiguration :
    IFieldHostURL,
    ITrajectoryHostURL,
    ICartographicProjectionHostURL,
    IUnitConversionHostURL
{
}
