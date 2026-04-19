using OSDC.UnitConversion.DrillingRazorMudComponents;

namespace NORCE.Drilling.Field.WebPages;

public static class DataUtils
{
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_Field = "Default Field Name";
    public static string DEFAULT_DESCR_Field = "Default Field Description";
    public static string DEFAULT_NAME_CartographicConversionSet = "Default CartographicConversionSet Name";
    public static string DEFAULT_DESCR_CartographicConversionSet = "Default CartographicConversionSet Description";

    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; } = "WGS84";
        public static string? PositionReferenceName { get; set; } = "WGS84";
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = val;
    }

    public static void UpdateDepthReferenceName(string value)
    {
        UnitAndReferenceParameters.DepthReferenceName = value;
    }

    public static void UpdatePositionReferenceName(string value)
    {
        UnitAndReferenceParameters.PositionReferenceName = value;
    }

    public static GroundMudLineDepthReferenceSource GroundMudLineDepthReferenceSource { get; } = new();
    public static SeaWaterLevelDepthReferenceSource SeaWaterLevelDepthReferenceSource { get; } = new();
    public static RotaryTableDepthReferenceSource RotaryTableDepthReferenceSource { get; } = new();
    public static WellHeadPositionReferenceSource WellHeadPositionReferenceSource { get; } = new();
    public static CartographicGridPositionReferenceSource CartographicGridPositionReferenceSource { get; } = new();
    public static LeaseLinePositionReferenceSource LeaseLinePositionReferenceSource { get; } = new();
    public static ClusterPositionReferenceSource ClusterPositionReferenceSource { get; } = new();

    public static readonly string FieldOutputParamLabel = "FieldOutputParam";
    public static readonly string FieldNameLabel = "Field name";
    public static readonly string FieldDescrLabel = "Field description";
    public static readonly string FieldOutputParamQty = "DepthDrilling";
}

public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
{
    public double? GroundMudLineDepthReference { get; set; }
}

public class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
{
    public double? RotaryTableDepthReference { get; set; }
}

public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
{
    public double? SeaWaterLevelDepthReference { get; set; }
}

public class WellHeadPositionReferenceSource : IWellHeadPositionReferenceSource
{
    public double? WellHeadNorthPositionReference { get; set; }
    public double? WellHeadEastPositionReference { get; set; }
}

public class CartographicGridPositionReferenceSource : ICartographicGridPositionReferenceSource
{
    public double? CartographicGridNorthPositionReference { get; set; }
    public double? CartographicGridEastPositionReference { get; set; }
}

public class LeaseLinePositionReferenceSource : ILeaseLinePositionReferenceSource
{
    public double? LeaseLineNorthPositionReference { get; set; }
    public double? LeaseLineEastPositionReference { get; set; }
}

public class ClusterPositionReferenceSource : IClusterPositionReferenceSource
{
    public double? ClusterNorthPositionReference { get; set; }
    public double? ClusterEastPositionReference { get; set; }
}
