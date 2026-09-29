using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSDC.Drilling.Field.Model;

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

[Semantic(Concepts.GeographicPosition2D)]
public sealed class FieldGeographicCoordinate
{
    [Semantic(Concepts.Latitude)]
    public double Latitude { get; set; }
    [Semantic(Concepts.Longitude)]
    public double Longitude { get; set; }
}

[Semantic(Concepts.ProjectedPosition2D)]
public sealed class FieldProjectedCoordinate
{
    [Semantic(Concepts.Easting, Reference = Concepts.ProjectedEastingNorthingSI)]
    public double Easting { get; set; }
    [Semantic(Concepts.Northing, Reference = Concepts.ProjectedEastingNorthingSI)]
    public double Northing { get; set; }
}

[Semantic(Concepts.Position)]
public sealed class FieldForwardConversionPosition
{
    [Semantic(Concepts.Latitude)]
    public double Latitude { get; set; }
    [Semantic(Concepts.Longitude)]
    public double Longitude { get; set; }
    [Semantic(Concepts.EllipsoidalDepth)]
    [System.ComponentModel.Description("SI metres, positive downward from the selected ellipsoid; read the enclosing datum and depth-transformation policy. A retained 2D depth is not proof of a vertical transformation.")]
    public double VerticalDepth { get; set; }
    [Semantic(Concepts.CoordinateEpoch, Reference = Concepts.Utc)]
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
}

public sealed class FieldInverseConversionPosition
{
    [Semantic(Concepts.Easting, Reference = Concepts.ProjectedEastingNorthingSI)]
    public double Easting { get; set; }
    [Semantic(Concepts.Northing, Reference = Concepts.ProjectedEastingNorthingSI)]
    public double Northing { get; set; }
    [Semantic(Concepts.EllipsoidalDepth)]
    [System.ComponentModel.Description("SI metres, positive downward from the selected ellipsoid; read the enclosing datum and depth-transformation policy. A retained 2D depth is not proof of a vertical transformation.")]
    public double VerticalDepth { get; set; }
    [Semantic(Concepts.CoordinateEpoch, Reference = Concepts.Utc)]
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
}

public sealed class FieldTransformationOptions
{
    public FieldTransformationSelectionPolicy SelectionPolicy { get; set; } = FieldTransformationSelectionPolicy.RequireUnambiguous;
    [Semantic(Concepts.ResourceIdentifier)]
    public List<Guid>? TransformationPathIDs { get; set; }
    [Semantic(Concepts.TransformationSelectionToken)]
    public string? SelectionToken { get; set; }
    public FieldApplicabilityPolicy ApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldDepthTransformationPolicy DepthPolicy { get; set; } = FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D;
}

[Semantic(Concepts.FieldCoordinateConversionRequest)]
public sealed class FieldForwardConversionRequest
{
    [Semantic(Concepts.ResourceIdentifier)]
    public Guid FieldID { get; set; }
    public FieldGeographicReference SourceGeographicReference { get; set; } = FieldGeographicReference.ProjectionDatum;
    public FieldApplicabilityPolicy ProjectionApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldTransformationOptions? Transformation { get; set; }

    [Required, MinLength(1)]
    public List<FieldForwardConversionPosition> Positions { get; set; } = [];
}

[Semantic(Concepts.FieldCoordinateConversionRequest)]
public sealed class FieldInverseConversionRequest
{
    [Semantic(Concepts.ResourceIdentifier)]
    public Guid FieldID { get; set; }
    public FieldApplicabilityPolicy ProjectionApplicabilityPolicy { get; set; } = FieldApplicabilityPolicy.RequireApplicable;
    public FieldTransformationOptions? Transformation { get; set; }

    [Required, MinLength(1)]
    public List<FieldInverseConversionPosition> Positions { get; set; } = [];
}

public sealed class FieldCatalogReference
{
    [Semantic(Concepts.ResourceIdentifier)]
    public Guid ID { get; set; }
    [Semantic(Concepts.ResourceName)]
    public string? Name { get; set; }
    public string? Authority { get; set; }
    public string? Code { get; set; }
}

[Semantic(Concepts.FieldCoordinateConversionResult)]
public sealed class FieldCoordinateConversionPositionResult
{
    public int PositionIndex { get; set; }
    [Semantic(Concepts.GeographicPosition2D)]
    public required FieldGeographicCoordinate ProjectionDatumGeographicCoordinate { get; set; }
    [Semantic(Concepts.GeographicPosition2D, Reference = Concepts.Wgs84)]
    public FieldGeographicCoordinate? Wgs84GeographicCoordinate { get; set; }
    [Semantic(Concepts.ProjectedPosition2D, Reference = Concepts.ProjectedEastingNorthingSI)]
    public required FieldProjectedCoordinate ProjectedCoordinate { get; set; }
    [Semantic(Concepts.EllipsoidalDepth)]
    [System.ComponentModel.Description("SI metres, positive downward from the selected ellipsoid; read the enclosing datum and depth-transformation policy. A retained 2D depth is not proof of a vertical transformation.")]
    public double ProjectionDatumVerticalDepth { get; set; }
    [Semantic(Concepts.EllipsoidalDepth, Reference = Concepts.Wgs84)]
    [System.ComponentModel.Description("SI metres, positive downward from the selected ellipsoid; read the enclosing datum and depth-transformation policy. A retained 2D depth is not proof of a vertical transformation.")]
    public double? Wgs84VerticalDepth { get; set; }
    [Semantic(Concepts.CoordinateEpoch, Reference = Concepts.Utc)]
    public DateTimeOffset? CoordinateEpochUtc { get; set; }
    [Semantic(Concepts.GridConvergence, Reference = Concepts.GridConvergenceTrueToGridClockwise)]
    public double? GridConvergence { get; set; }
}

public sealed class FieldConversionWarning
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}

[Semantic(Concepts.FieldCoordinateConversionResult)]
public sealed class FieldCoordinateConversionResponse
{
    [Semantic(Concepts.ResourceIdentifier)]
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
