using NORCE.Drilling.Field.WebPages;

namespace NORCE.Drilling.Field.WebApp;

public class WebPagesHostConfiguration : IFieldWebPagesConfiguration
{
    public string FieldHostURL { get; set; } = string.Empty;
    public string CartographicProjectionHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
}
