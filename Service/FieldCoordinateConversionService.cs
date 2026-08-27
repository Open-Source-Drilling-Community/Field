using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.ModelShared;
using OSDC.Drilling.Field.Service.Managers;

namespace OSDC.Drilling.Field.Service;

public sealed class FieldConversionException(
    int statusCode,
    string error,
    string message,
    IEnumerable<FieldConversionValidationError>? errors = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    public FieldConversionErrorEnvelope Envelope { get; } = new()
    {
        Error = error,
        Message = message,
        Errors = errors?.ToList() ?? []
    };
}

public sealed class FieldCoordinateConversionService
{
    public static readonly Guid Wgs84DatumID = Guid.Parse("00000000-0000-4000-8000-000000006326");
    private const int MaximumPositions = 10_000;
    private readonly FieldManager fieldManager;
    private readonly IEarthCartographicProjectionClient projectionClient;
    private readonly IEarthGeodesyClient geodesyClient;
    private readonly ILogger<FieldCoordinateConversionService> logger;

    public FieldCoordinateConversionService(
        ILogger<FieldManager> fieldManagerLogger,
        SqlConnectionManager connectionManager,
        IEarthCartographicProjectionClient projectionClient,
        IEarthGeodesyClient geodesyClient,
        ILogger<FieldCoordinateConversionService> logger)
    {
        fieldManager = FieldManager.GetInstance(fieldManagerLogger, connectionManager);
        this.projectionClient = projectionClient;
        this.geodesyClient = geodesyClient;
        this.logger = logger;
    }

    public async Task<FieldCoordinateConversionResponse> ForwardAsync(FieldForwardConversionRequest request, CancellationToken cancellationToken)
    {
        ValidateForward(request);
        (Model.Field field, ProjectionDefinition definition, Guid datumId) = await ResolveFieldAsync(request.FieldID, cancellationToken);
        FieldTransformationOptions options = request.Transformation ?? new();

        List<GeodeticPosition> projectionDatumPositions;
        List<GeodeticPosition>? wgs84Positions;
        List<FieldConversionWarning> warnings = [];

        if (request.SourceGeographicReference == FieldGeographicReference.Wgs84)
        {
            wgs84Positions = request.Positions.Select(ToGeodeticPosition).ToList();
            projectionDatumPositions = datumId == Wgs84DatumID
                ? Copy(wgs84Positions)
                : await TransformRequiredAsync(Wgs84DatumID, datumId, wgs84Positions, options, warnings, cancellationToken);
        }
        else
        {
            projectionDatumPositions = request.Positions.Select(ToGeodeticPosition).ToList();
            wgs84Positions = datumId == Wgs84DatumID
                ? Copy(projectionDatumPositions)
                : await TransformOptionalAsync(datumId, Wgs84DatumID, projectionDatumPositions, options, warnings, cancellationToken);
        }

        ForwardProjectionResponse projected;
        try
        {
            projected = await projectionClient.ForwardAsync(new ForwardProjectionRequest
            {
                ProjectionDefinitionId = field.ProjectionDefinitionID!.Value,
                ApplicabilityPolicy = ToDependency(request.ProjectionApplicabilityPolicy),
                Positions = projectionDatumPositions.Select(position => new GeographicCoordinate
                {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude
                }).ToList()
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw ProjectionDependencyFailure("projection_failed", "EarthCartographicProjection rejected or failed the forward projection.", exception);
        }

        ValidateDependencyCount(projected.Positions, request.Positions.Count, "EarthCartographicProjection forward response");
        AddWarnings(warnings, projected.Warnings);
        List<ForwardProjectionPosition> projectedPositions = projected.Positions.OrderBy(position => position.PositionIndex).ToList();
        return BuildResponse(field, definition, datumId, projected.ApiAxisConvention, projectedPositions.Select((position, index) =>
            new FieldCoordinateConversionPositionResult
            {
                PositionIndex = index,
                ProjectionDatumGeographicCoordinate = ToField(position.GeographicCoordinate),
                Wgs84GeographicCoordinate = wgs84Positions == null ? null : ToField(wgs84Positions[index]),
                ProjectedCoordinate = ToField(position.ProjectedCoordinate),
                ProjectionDatumVerticalDepth = projectionDatumPositions[index].Depth,
                Wgs84VerticalDepth = wgs84Positions?[index].Depth,
                CoordinateEpochUtc = request.Positions[index].CoordinateEpochUtc,
                GridConvergence = position.GridConvergence
            }).ToList(), warnings);
    }

    public async Task<FieldCoordinateConversionResponse> InverseAsync(FieldInverseConversionRequest request, CancellationToken cancellationToken)
    {
        ValidateInverse(request);
        (Model.Field field, ProjectionDefinition definition, Guid datumId) = await ResolveFieldAsync(request.FieldID, cancellationToken);
        FieldTransformationOptions options = request.Transformation ?? new();

        InverseProjectionResponse inverse;
        try
        {
            inverse = await projectionClient.InverseAsync(new InverseProjectionRequest
            {
                ProjectionDefinitionId = field.ProjectionDefinitionID!.Value,
                ApplicabilityPolicy = ToDependency(request.ProjectionApplicabilityPolicy),
                Positions = request.Positions.Select(position => new ProjectedCoordinate
                {
                    Easting = position.Easting,
                    Northing = position.Northing
                }).ToList()
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw ProjectionDependencyFailure("inverse_projection_failed", "EarthCartographicProjection rejected or failed the inverse projection.", exception);
        }

        ValidateDependencyCount(inverse.Positions, request.Positions.Count, "EarthCartographicProjection inverse response");
        List<InverseProjectionPosition> inversePositions = inverse.Positions.OrderBy(position => position.PositionIndex).ToList();
        List<GeodeticPosition> projectionDatumPositions = inversePositions.Select((position, index) => new GeodeticPosition
        {
            Latitude = position.GeographicCoordinate.Latitude,
            Longitude = position.GeographicCoordinate.Longitude,
            Depth = request.Positions[index].VerticalDepth,
            CoordinateEpochUtc = request.Positions[index].CoordinateEpochUtc
        }).ToList();
        List<FieldConversionWarning> warnings = [];
        AddWarnings(warnings, inverse.Warnings);
        List<GeodeticPosition>? wgs84Positions = datumId == Wgs84DatumID
            ? Copy(projectionDatumPositions)
            : await TransformOptionalAsync(datumId, Wgs84DatumID, projectionDatumPositions, options, warnings, cancellationToken);

        return BuildResponse(field, definition, datumId, inverse.ApiAxisConvention, inversePositions.Select((position, index) =>
            new FieldCoordinateConversionPositionResult
            {
                PositionIndex = index,
                ProjectionDatumGeographicCoordinate = ToField(position.GeographicCoordinate),
                Wgs84GeographicCoordinate = wgs84Positions == null ? null : ToField(wgs84Positions[index]),
                ProjectedCoordinate = ToField(position.ProjectedCoordinate),
                ProjectionDatumVerticalDepth = projectionDatumPositions[index].Depth,
                Wgs84VerticalDepth = wgs84Positions?[index].Depth,
                CoordinateEpochUtc = request.Positions[index].CoordinateEpochUtc,
                GridConvergence = position.GridConvergence
            }).ToList(), warnings);
    }

    private async Task<(Model.Field Field, ProjectionDefinition Definition, Guid DatumId)> ResolveFieldAsync(Guid fieldId, CancellationToken cancellationToken)
    {
        Model.Field? field = fieldManager.GetFieldById(fieldId);
        if (field == null)
            throw new FieldConversionException(404, "field_not_found", $"Field '{fieldId}' was not found.");
        if (!field.ProjectionDefinitionID.HasValue || field.ProjectionDefinitionID == Guid.Empty)
            throw new FieldConversionException(422, "projection_definition_missing", "The Field does not reference an EarthCartographicProjection definition.");

        ProjectionDefinition definition;
        try { definition = await projectionClient.GetDefinitionAsync(field.ProjectionDefinitionID.Value, cancellationToken); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw DependencyFailure("projection_definition_unavailable", "The Field's projection definition could not be retrieved.", exception);
        }

        Guid? datumId = definition.BaseGeographicCrs?.Datum?.EarthGeodesyDatumId;
        if (!datumId.HasValue || datumId == Guid.Empty)
            throw new FieldConversionException(422, "projection_datum_unavailable", "The projection definition has no verified EarthGeodesy datum UUID.");
        return (field, definition, datumId.Value);
    }

    private async Task<List<GeodeticPosition>> TransformRequiredAsync(Guid source, Guid target, List<GeodeticPosition> positions, FieldTransformationOptions options, List<FieldConversionWarning> warnings, CancellationToken cancellationToken)
    {
        try
        {
            TransformCoordinatesResponse response = await TransformAsync(source, target, positions, options, warnings, cancellationToken);
            ValidateDependencyCount(response.Positions, positions.Count, "EarthGeodesy transform response");
            AddWarnings(warnings, response.Warnings);
            return response.Positions.ToList();
        }
        catch (FieldConversionException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw DependencyFailure("geodetic_transformation_required", "No usable EarthGeodesy transformation completed from WGS 84 to the projection datum.", exception);
        }
    }

    private async Task<List<GeodeticPosition>?> TransformOptionalAsync(Guid source, Guid target, List<GeodeticPosition> positions, FieldTransformationOptions options, List<FieldConversionWarning> warnings, CancellationToken cancellationToken)
    {
        try
        {
            TransformCoordinatesResponse response = await TransformAsync(source, target, positions, options, warnings, cancellationToken);
            ValidateDependencyCount(response.Positions, positions.Count, "EarthGeodesy transform response");
            AddWarnings(warnings, response.Warnings);
            return response.Positions.ToList();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Optional transformation from datum {SourceDatumId} to {TargetDatumId} is unavailable", source, target);
            warnings.Add(new FieldConversionWarning
            {
                Code = "geodetic_conversion_path_unavailable",
                Message = "Projection succeeded, but WGS 84 coordinates are unavailable because no usable EarthGeodesy transformation completed."
            });
            return null;
        }
    }

    private async Task<TransformCoordinatesResponse> TransformAsync(
        Guid source,
        Guid target,
        List<GeodeticPosition> positions,
        FieldTransformationOptions options,
        List<FieldConversionWarning> warnings,
        CancellationToken cancellationToken)
    {
        TransformCoordinatesRequest request = CreateTransformRequest(source, target, positions, options);
        if (options.SelectionPolicy == FieldTransformationSelectionPolicy.FirstAvailable)
        {
            ResolveTransformationPathsResponse paths = await geodesyClient.ResolvePathsAsync(new ResolveTransformationPathsRequest
            {
                SourceDatumId = source,
                TargetDatumId = target,
                Positions = positions,
                MaximumCandidates = 50
            }, cancellationToken);
            TransformationPathCandidate? selected = paths.Candidates?
                .Where(candidate => candidate.IsExecutable && !string.IsNullOrWhiteSpace(candidate.SelectionToken))
                .OrderBy(candidate => candidate.Rank)
                .FirstOrDefault();
            if (selected == null)
                throw new FieldConversionException(502, "geodetic_transformation_path_unavailable",
                    "EarthGeodesy found no position-applicable transformation path executable by its current runtime.");

            request.SelectionToken = selected.SelectionToken;
            request.SelectionPolicy = TransformationSelectionPolicy.RequireUnambiguous;
            request.TransformationPathIds = null;
            string operationNames = string.Join(" -> ", selected.Operations?.Select(operation => operation.Name) ?? []);
            warnings.Add(new FieldConversionWarning
            {
                Code = "geodetic_transformation_path_selected_automatically",
                Message = $"EarthGeodesy selected ranked path {selected.Rank}{(selected.CombinedAccuracy.HasValue ? $" with combined accuracy {selected.CombinedAccuracy.Value:G} metres" : string.Empty)}: {operationNames}."
            });
        }
        return await geodesyClient.TransformAsync(request, cancellationToken);
    }

    private static TransformCoordinatesRequest CreateTransformRequest(Guid source, Guid target, List<GeodeticPosition> positions, FieldTransformationOptions options) => new()
    {
        SourceDatumId = source,
        TargetDatumId = target,
        Positions = positions,
        SelectionPolicy = options.SelectionPolicy switch
        {
            FieldTransformationSelectionPolicy.FirstAvailable => TransformationSelectionPolicy.FirstAvailable,
            FieldTransformationSelectionPolicy.ExplicitPath => TransformationSelectionPolicy.ExplicitPath,
            _ => TransformationSelectionPolicy.RequireUnambiguous
        },
        TransformationPathIds = options.TransformationPathIDs,
        SelectionToken = options.SelectionToken,
        ApplicabilityPolicy = ToDependency(options.ApplicabilityPolicy),
        DepthPolicy = options.DepthPolicy == FieldDepthTransformationPolicy.PreservePhysicalPoint
            ? DepthTransformationPolicy.PreservePhysicalPoint
            : DepthTransformationPolicy.AllowUntransformedDepthFor2D
    };

    private static void ValidateForward(FieldForwardConversionRequest request)
    {
        if (request.FieldID == Guid.Empty) throw Validation("FieldID", "field_id_required", "FieldID must be a non-empty UUID.");
        ValidateCount(request.Positions?.Count ?? 0);
        IReadOnlyList<FieldForwardConversionPosition> positions = request.Positions!;
        List<FieldConversionValidationError> errors = [];
        for (int index = 0; index < positions.Count; index++)
        {
            FieldForwardConversionPosition position = positions[index];
            ValidateFinite(errors, index, nameof(position.Latitude), position.Latitude);
            ValidateFinite(errors, index, nameof(position.Longitude), position.Longitude);
            ValidateFinite(errors, index, nameof(position.VerticalDepth), position.VerticalDepth);
            if (double.IsFinite(position.Latitude) && (position.Latitude < -Math.PI / 2 || position.Latitude > Math.PI / 2))
                errors.Add(Error(index, nameof(position.Latitude), "latitude_out_of_range", "Latitude must be between -pi/2 and pi/2 SI radians."));
            if (double.IsFinite(position.Longitude) && (position.Longitude < -Math.PI || position.Longitude > Math.PI))
                errors.Add(Error(index, nameof(position.Longitude), "longitude_out_of_range", "Longitude must be between -pi and pi SI radians."));
        }
        ValidateTransformation(request.Transformation, errors);
        if (errors.Count > 0) throw new FieldConversionException(400, "validation_failed", "The complete batch was rejected.", errors);
    }

    private static void ValidateInverse(FieldInverseConversionRequest request)
    {
        if (request.FieldID == Guid.Empty) throw Validation("FieldID", "field_id_required", "FieldID must be a non-empty UUID.");
        ValidateCount(request.Positions?.Count ?? 0);
        IReadOnlyList<FieldInverseConversionPosition> positions = request.Positions!;
        List<FieldConversionValidationError> errors = [];
        for (int index = 0; index < positions.Count; index++)
        {
            FieldInverseConversionPosition position = positions[index];
            ValidateFinite(errors, index, nameof(position.Easting), position.Easting);
            ValidateFinite(errors, index, nameof(position.Northing), position.Northing);
            ValidateFinite(errors, index, nameof(position.VerticalDepth), position.VerticalDepth);
        }
        ValidateTransformation(request.Transformation, errors);
        if (errors.Count > 0) throw new FieldConversionException(400, "validation_failed", "The complete batch was rejected.", errors);
    }

    private static void ValidateCount(int count)
    {
        if (count < 1 || count > MaximumPositions)
            throw Validation("Positions", "positions_count_invalid", $"Positions must contain between 1 and {MaximumPositions} items.");
    }

    private static void ValidateTransformation(FieldTransformationOptions? options, List<FieldConversionValidationError> errors)
    {
        if (options == null) return;
        if (options.SelectionPolicy == FieldTransformationSelectionPolicy.ExplicitPath &&
            (options.TransformationPathIDs == null || options.TransformationPathIDs.Count == 0) && string.IsNullOrWhiteSpace(options.SelectionToken))
            errors.Add(Error(null, "Transformation", "explicit_path_required", "ExplicitPath requires TransformationPathIDs or a SelectionToken."));
        if (options.TransformationPathIDs?.Any(id => id == Guid.Empty) == true)
            errors.Add(Error(null, "Transformation.TransformationPathIDs", "empty_transformation_id", "Transformation path UUIDs must be non-empty."));
    }

    private static void ValidateFinite(List<FieldConversionValidationError> errors, int index, string property, double value)
    {
        if (!double.IsFinite(value)) errors.Add(Error(index, property, "non_finite_value", $"{property} must be finite."));
    }

    private static void ValidateDependencyCount<T>(ICollection<T>? positions, int expected, string source)
    {
        if (positions == null || positions.Count != expected)
            throw new FieldConversionException(502, "dependency_contract_violation", $"{source} returned {positions?.Count ?? 0} positions; {expected} were required.");
    }

    private static FieldCoordinateConversionResponse BuildResponse(Model.Field field, ProjectionDefinition definition, Guid datumId, string? axisConvention, List<FieldCoordinateConversionPositionResult> positions, List<FieldConversionWarning> warnings) => new()
    {
        FieldID = field.MetaInfo!.ID,
        ProjectionDefinition = new FieldCatalogReference
        {
            ID = definition.Id,
            Name = definition.Name,
            Authority = definition.Identifier?.Authority,
            Code = definition.Identifier?.Code
        },
        ProjectionDatum = new FieldCatalogReference
        {
            ID = datumId,
            Name = definition.BaseGeographicCrs.Datum.Name,
            Authority = definition.BaseGeographicCrs.Datum.Identifier?.Authority,
            Code = definition.BaseGeographicCrs.Datum.Identifier?.Code
        },
        Wgs84Datum = new FieldCatalogReference { ID = Wgs84DatumID, Name = "WGS 84", Authority = "EPSG", Code = "6326" },
        ApiAxisConvention = string.IsNullOrWhiteSpace(axisConvention) ? "easting, then northing; SI metres" : axisConvention,
        Positions = positions,
        Warnings = warnings
    };

    private static void AddWarnings(List<FieldConversionWarning> target, IEnumerable<ServiceWarning>? warnings)
    {
        if (warnings == null) return;
        target.AddRange(warnings.Select(warning => new FieldConversionWarning
        {
            Code = string.IsNullOrWhiteSpace(warning.Code) ? "dependency_warning" : warning.Code,
            Message = warning.Message ?? string.Empty
        }));
    }

    private static List<GeodeticPosition> Copy(IEnumerable<GeodeticPosition> positions) => positions.Select(position => new GeodeticPosition
    {
        Latitude = position.Latitude,
        Longitude = position.Longitude,
        Depth = position.Depth,
        CoordinateEpochUtc = position.CoordinateEpochUtc
    }).ToList();

    private static GeodeticPosition ToGeodeticPosition(FieldForwardConversionPosition position) => new()
    {
        Latitude = position.Latitude,
        Longitude = position.Longitude,
        Depth = position.VerticalDepth,
        CoordinateEpochUtc = position.CoordinateEpochUtc
    };

    private static FieldGeographicCoordinate ToField(GeographicCoordinate coordinate) => new() { Latitude = coordinate.Latitude, Longitude = coordinate.Longitude };
    private static FieldGeographicCoordinate ToField(GeodeticPosition coordinate) => new() { Latitude = coordinate.Latitude, Longitude = coordinate.Longitude };
    private static FieldProjectedCoordinate ToField(ProjectedCoordinate coordinate) => new() { Easting = coordinate.Easting, Northing = coordinate.Northing };
    private static ApplicabilityPolicy ToDependency(FieldApplicabilityPolicy policy) => policy == FieldApplicabilityPolicy.AllowUnknown ? ApplicabilityPolicy.AllowUnknown : ApplicabilityPolicy.RequireApplicable;
    private static FieldConversionException ProjectionDependencyFailure(string code, string message, Exception exception)
    {
        if (exception is ApiException apiException && !string.IsNullOrWhiteSpace(apiException.Response))
        {
            try
            {
                FieldConversionErrorEnvelope? problem = JsonSerializer.Deserialize<FieldConversionErrorEnvelope>(
                    apiException.Response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (problem != null)
                {
                    int statusCode = apiException.StatusCode is 400 or 404 or 409 or 422 ? apiException.StatusCode : 502;
                    return new FieldConversionException(
                        statusCode,
                        string.IsNullOrWhiteSpace(problem.Error) ? code : problem.Error,
                        string.IsNullOrWhiteSpace(problem.Message) ? message : problem.Message,
                        problem.Errors,
                        exception);
                }
            }
            catch (JsonException)
            {
                // Fall through to the stable dependency-failure envelope.
            }
        }
        return DependencyFailure(code, message, exception);
    }
    private static FieldConversionException DependencyFailure(string code, string message, Exception exception) => new(502, code, message, null, exception);
    private static FieldConversionException Validation(string property, string code, string message) => new(400, "validation_failed", "The complete batch was rejected.", [Error(null, property, code, message)]);
    private static FieldConversionValidationError Error(int? index, string property, string code, string message) => new() { PositionIndex = index, Property = property, Code = code, Message = message };
}
