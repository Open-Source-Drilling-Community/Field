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
            .Where(cluster => cluster is not null && cluster.FieldID == fieldId)
            .ToList() ?? [];

        List<(double Latitude, double Longitude)> datumPositions = fieldClusters
            .Where(cluster => cluster.ReferencePoint?.Latitude != null && cluster.ReferencePoint.Longitude != null)
            .Select(cluster => (cluster.ReferencePoint!.Latitude!.Value, cluster.ReferencePoint.Longitude!.Value))
            .ToList();
        if (datumPositions.Count == 0)
        {
            datumPositions = fieldClusters.SelectMany(cluster => cluster.Slots?.Values ?? [])
                .Where(slot => slot.Latitude?.GaussianValue?.Mean != null && slot.Longitude?.GaussianValue?.Mean != null)
                .Select(slot => (slot.Latitude!.GaussianValue!.Mean!.Value, slot.Longitude!.GaussianValue!.Mean!.Value))
                .ToList();
        }

        double? averageLatitude = Average(datumPositions.Select(position => (double?)position.Latitude));
        double? averageLongitude = Average(datumPositions.Select(position => (double?)position.Longitude));
        double? averageTopWaterDepth = Average(fieldClusters.Select(cluster => cluster.TopWaterDepth?.GaussianValue?.Mean));

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
        DataUtils.FieldPositionReferenceSource.FieldNorthPositionReference = null;
        DataUtils.FieldPositionReferenceSource.FieldEastPositionReference = null;
        if (string.Equals(DataUtils.UnitAndReferenceParameters.PositionReferenceName, "Field", StringComparison.Ordinal))
        {
            DataUtils.UnitAndReferenceParameters.PositionReferenceName = "WGS84";
        }
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
        return -response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth;
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
