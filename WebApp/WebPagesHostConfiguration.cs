using NORCE.Drilling.Field.WebPages;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Field.WebApp;

public class WebPagesHostConfiguration : IFieldWebPagesConfiguration
{
    public string FieldHostURL { get; set; } = string.Empty;
    public string ClusterHostURL { get; set; } = string.Empty;
    public string WellHostURL { get; set; } = string.Empty;
    public string WellBoreHostURL { get; set; } = string.Empty;
    public string TrajectoryHostURL { get; set; } = string.Empty;
    public string CartographicProjectionHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
}
