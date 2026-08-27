using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NORCE.Drilling.Field.Model;

public enum FieldGeographicReference
{
    ProjectionDatum,
    Wgs84
}

public enum FieldApplicabilityPolicy
{
    RequireApplicable,
    AllowUnknown
}

public enum FieldTransformationSelectionPolicy
{
    RequireUnambiguous,
    FirstAvailable,
    ExplicitPath
}

public enum FieldDepthTransformationPolicy
{
    PreservePhysicalPoint,
    AllowUntransformedDepthFor2D
}

public sealed class FieldGeographicCoordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class FieldProjectedCoordinate
{
    public double Easting { get; set; }
    public double Northing { get; set; }
}

public sealed class FieldForwardConversionPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double VerticalDepth { get; set; }
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
}

public sealed class FieldInverseConversionPosition
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double VerticalDepth { get; set; }
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
}

public sealed class FieldTransformationOptions
{
    public FieldTransformationSelectionPolicy SelectionPolicy { get; set; } = FieldTransformationSelectionPolicy.RequireUnambiguous;
    public List<Guid>? TransformationPathIDs { get; set; }
    public string? SelectionToken { get; set; }
    public FieldApplicabilityPolicy ApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldDepthTransformationPolicy DepthPolicy { get; set; } = FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D;
}

public sealed class FieldForwardConversionRequest
{
    public Guid FieldID { get; set; }
    public FieldGeographicReference SourceGeographicReference { get; set; } = FieldGeographicReference.ProjectionDatum;
    public FieldApplicabilityPolicy ProjectionApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldTransformationOptions? Transformation { get; set; }

    [Required, MinLength(1)]
    public List<FieldForwardConversionPosition> Positions { get; set; } = [];
}

public sealed class FieldInverseConversionRequest
{
    public Guid FieldID { get; set; }
    public FieldApplicabilityPolicy ProjectionApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldTransformationOptions? Transformation { get; set; }

    [Required, MinLength(1)]
    public List<FieldInverseConversionPosition> Positions { get; set; } = [];
}

public sealed class FieldCatalogReference
{
    public Guid ID { get; set; }
    public string? Name { get; set; }
    public string? Authority { get; set; }
    public string? Code { get; set; }
}

public sealed class FieldCoordinateConversionPositionResult
{
    public int PositionIndex { get; set; }
    public required FieldGeographicCoordinate ProjectionDatumGeographicCoordinate { get; set; }
    public FieldGeographicCoordinate? Wgs84GeographicCoordinate { get; set; }
    public required FieldProjectedCoordinate ProjectedCoordinate { get; set; }
    public double ProjectionDatumVerticalDepth { get; set; }
    public double? Wgs84VerticalDepth { get; set; }
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
    public double? GridConvergence { get; set; }
}

public sealed class FieldConversionWarning
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}

public sealed class FieldCoordinateConversionResponse
{
    public Guid FieldID { get; set; }
    public required FieldCatalogReference ProjectionDefinition { get; set; }
    public required FieldCatalogReference ProjectionDatum { get; set; }
    public required FieldCatalogReference Wgs84Datum { get; set; }
    public required string ApiAxisConvention { get; set; }
    public List<FieldCoordinateConversionPositionResult> Positions { get; set; } = [];
    public List<FieldConversionWarning> Warnings { get; set; } = [];
}

public sealed class FieldConversionValidationError
{
    public int? PositionIndex { get; set; }
    public required string Property { get; set; }
    public required string Code { get; set; }
    public required string Message { get; set; }
}

public sealed class FieldConversionErrorEnvelope
{
    public required string Error { get; set; }
    public required string Message { get; set; }
    public List<FieldConversionValidationError> Errors { get; set; } = [];
}
