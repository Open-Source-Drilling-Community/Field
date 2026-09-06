using Microsoft.Extensions.Logging;
using FieldModelShared = OSDC.Drilling.Field.ModelShared;

namespace OSDC.Drilling.Field.WebPages;

internal static class CartographicPositionReferenceUtils
{
    public static async Task ApplyAsync(IFieldAPIUtils api, FieldModelShared.Field? field, ILogger logger)
    {
        CartographicGridPositionReferenceSource source = DataUtils.CartographicGridPositionReferenceSource;
        source.CartographicGridNorthPositionReference = null;
        source.CartographicGridEastPositionReference = null;

        if (field?.MetaInfo?.ID is not Guid fieldId || fieldId == Guid.Empty ||
            field.ReferencePoint?.Latitude is not double latitude ||
            field.ReferencePoint.Longitude is not double longitude ||
            field.ReferencePoint.RiemannianNorth is not double riemannianNorth ||
            field.ReferencePoint.RiemannianEast is not double riemannianEast)
        {
            ResetUnavailableSelection();
            return;
        }

        try
        {
            FieldModelShared.FieldCoordinateConversionResponse response = await api.ClientField.ForwardFieldCoordinatesAsync(new FieldModelShared.FieldForwardConversionRequest
            {
                FieldID = fieldId,
                SourceGeographicReference = FieldModelShared.FieldGeographicReference.Wgs84,
                ProjectionApplicabilityPolicy = FieldModelShared.FieldApplicabilityPolicy.AllowUnknown,
                Transformation = new FieldModelShared.FieldTransformationOptions
                {
                    SelectionPolicy = FieldModelShared.FieldTransformationSelectionPolicy.FirstAvailable,
                    ApplicabilityPolicy = FieldModelShared.FieldApplicabilityPolicy.AllowUnknown,
                    DepthPolicy = FieldModelShared.FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D
                },
                Positions =
                [
                    new FieldModelShared.FieldForwardConversionPosition
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        VerticalDepth = field.ReferencePoint.TVD ?? 0
                    }
                ]
            });

            FieldModelShared.FieldCoordinateConversionPositionResult? result = response.Positions?.FirstOrDefault();
            if (result?.ProjectedCoordinate == null)
            {
                ResetUnavailableSelection();
                return;
            }

            source.CartographicGridNorthPositionReference = result.ProjectedCoordinate.Northing - riemannianNorth;
            source.CartographicGridEastPositionReference = result.ProjectedCoordinate.Easting - riemannianEast;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to resolve the cartographic position reference for field {FieldId}", fieldId);
            ResetUnavailableSelection();
        }
    }

    private static void ResetUnavailableSelection()
    {
        if (string.Equals(DataUtils.UnitAndReferenceParameters.PositionReferenceName, "Cartographic", StringComparison.Ordinal))
        {
            DataUtils.UnitAndReferenceParameters.PositionReferenceName = "WGS84";
        }
    }
}
