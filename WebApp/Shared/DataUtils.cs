public static class DataUtils
{
    // default values
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_Field = "Default Field Name";
    public static string DEFAULT_DESCR_Field = "Default Field Description";
    public static string DEFAULT_NAME_CartographicConversionSet = "Default CartographicConversionSet Name";
    public static string DEFAULT_DESCR_CartographicConversionSet = "Default CartographicConversionSet Description";

    // unit management
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; }
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = (string)val;
    }

    // units and labels
    public static readonly string FieldOutputParamLabel = "FieldOutputParam";
    public static readonly string FieldNameLabel = "Field name";
    public static readonly string FieldDescrLabel = "Field description";
    public static readonly string FieldOutputParamQty = "DepthDrilling";
}