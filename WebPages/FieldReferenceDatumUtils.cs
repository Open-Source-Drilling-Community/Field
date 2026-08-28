using FieldModelShared = OSDC.Drilling.Field.ModelShared;

namespace OSDC.Drilling.Field.WebPages;

public readonly record struct FieldReferenceDatumValues(
    double? SeaWaterLevelDepthReference,
    double? MeanSeaLevelDepthReference);

public static class FieldReferenceDatumUtils
{
    public static async Task<FieldReferenceDatumValues> ResolveForFieldAsync(
        IFieldAPIUtils api,
        Guid? fieldId,
        IEnumerable<FieldModelShared.Cluster>? clusters)
    {
        List<FieldModelShared.Cluster> fieldClusters = clusters?
            .Where(cluster =>
                cluster is not null &&
                cluster.FieldID == fieldId &&
                cluster.ReferencePoint?.Latitude != null &&
                cluster.ReferencePoint?.Longitude != null)
            .ToList() ?? [];

        double? averageLatitude = Average(fieldClusters.Select(cluster => cluster.ReferencePoint?.Latitude));
        double? averageLongitude = Average(fieldClusters.Select(cluster => cluster.ReferencePoint?.Longitude));
        double? averageTopWaterDepth = Average(clusters?
            .Where(cluster => cluster is not null && cluster.FieldID == fieldId)
            .Select(cluster => cluster.TopWaterDepth?.GaussianValue?.Mean));

        double? meanSeaLevelReference = await CalculateMeanSeaLevelDepthReferenceAsync(api, averageLatitude, averageLongitude);

        return new FieldReferenceDatumValues(
            SeaWaterLevelDepthReference: averageTopWaterDepth is null ? null : -averageTopWaterDepth,
            MeanSeaLevelDepthReference: meanSeaLevelReference);
    }

    public static void Apply(FieldReferenceDatumValues values)
    {
        DataUtils.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = values.SeaWaterLevelDepthReference;
        DataUtils.MeanSeaLevelDepthReferenceSource.MeanSeaLevelDepthReference = values.MeanSeaLevelDepthReference;
    }

    public static void Clear()
    {
        DataUtils.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = null;
        DataUtils.MeanSeaLevelDepthReferenceSource.MeanSeaLevelDepthReference = null;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridNorthPositionReference = null;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridEastPositionReference = null;
        DataUtils.CartographicProjectionDatumGeodeticReferenceSource.CartographicProjectionDatumLatitudeReference = null;
        DataUtils.CartographicProjectionDatumGeodeticReferenceSource.CartographicProjectionDatumLongitudeReference = null;
    }

    public static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(IFieldAPIUtils api, double? latitude, double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        FieldModelShared.MeanSeaLevelToWgs84Request request = new()
        {
            Positions =
            [
                new FieldModelShared.EarthVerticalDatumPosition
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    MeanSeaLevelDepth = 0
                }
            ]
        };

        FieldModelShared.MeanSeaLevelToWgs84Response response =
            await api.ClientEarthVerticalDatum.ConvertMeanSeaLevelToWgs84Async(request);
        return response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth;
    }

    private static double? Average(IEnumerable<double?>? values)
    {
        List<double> knownValues = values?
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList() ?? [];

        return knownValues.Count == 0 ? null : knownValues.Average();
    }
}
