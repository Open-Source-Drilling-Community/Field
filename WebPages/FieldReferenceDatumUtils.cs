using FieldModelShared = NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.WebPages;

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
                cluster.ReferenceLatitude?.GaussianValue?.Mean != null &&
                cluster.ReferenceLongitude?.GaussianValue?.Mean != null)
            .ToList() ?? [];

        double? averageLatitude = Average(fieldClusters.Select(cluster => cluster.ReferenceLatitude?.GaussianValue?.Mean));
        double? averageLongitude = Average(fieldClusters.Select(cluster => cluster.ReferenceLongitude?.GaussianValue?.Mean));
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
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridNorthPositionReference = 0;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridEastPositionReference = 0;
    }

    public static void Clear()
    {
        DataUtils.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = null;
        DataUtils.MeanSeaLevelDepthReferenceSource.MeanSeaLevelDepthReference = null;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridNorthPositionReference = 0;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridEastPositionReference = 0;
    }

    public static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(IFieldAPIUtils api, double? latitude, double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        Guid orderId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FieldModelShared.VerticalDatumOrder order = new()
        {
            MetaInfo = CreateMetaInfo(orderId, api.HostNameVerticalDatum, api.HostBasePathVerticalDatum, "VerticalDatumOrder/"),
            Name = $"MSL reference {orderId}",
            Description = "Temporary MSL-to-WGS84 conversion.",
            CreationDate = now,
            LastModificationDate = now,
            VerticalDatum = new FieldModelShared.VerticalDatum
            {
                MetaInfo = CreateMetaInfo(Guid.NewGuid(), api.HostNameVerticalDatum, api.HostBasePathVerticalDatum, "VerticalDatum/"),
                Name = $"MSL reference {orderId}",
                Description = "Temporary MSL-to-WGS84 conversion.",
                CreationDate = now,
                LastModificationDate = now,
                DatumSet =
                [
                    new FieldModelShared.VerticalDatumSet
                    {
                        Latitude = latitude.Value,
                        Longitude = longitude.Value,
                        GenericVerticalDatum = 0
                    }
                ],
                ConversionFrom = FieldModelShared.VerticalDatumConversion.FromMeanSeaLevel,
                Type = FieldModelShared.VerticalDatumType.Raw
            }
        };

        try
        {
            await api.ClientVerticalDatum.PostVerticalDatumOrderAsync(order);
            FieldModelShared.VerticalDatumOrder completed = await api.ClientVerticalDatum.GetVerticalDatumOrderByIdAsync(orderId);
            double? verticalDatumWgs64 = completed.VerticalDatum?.DatumSet?.FirstOrDefault()?.VerticalDatumWGS64;
            return verticalDatumWgs64 is null ? null : -verticalDatumWgs64;
        }
        finally
        {
            try
            {
                await api.ClientVerticalDatum.DeleteVerticalDatumOrderByIdAsync(orderId);
            }
            catch
            {
            }
        }
    }

    private static FieldModelShared.MetaInfo CreateMetaInfo(Guid id, string hostName, string hostBasePath, string endPoint) =>
        new()
        {
            ID = id,
            HttpHostName = hostName,
            HttpHostBasePath = hostBasePath,
            HttpEndPoint = endPoint
        };

    private static double? Average(IEnumerable<double?>? values)
    {
        List<double> knownValues = values?
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList() ?? [];

        return knownValues.Count == 0 ? null : knownValues.Average();
    }
}
