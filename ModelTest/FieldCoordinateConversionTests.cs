using System;
using System.Text.Json;
using OSDC.Drilling.Field.Model;
using NUnit.Framework;

namespace OSDC.Drilling.Field.ModelTest;

public class FieldCoordinateConversionTests
{
    [Test]
    public void Forward_Request_Defaults_To_Projection_Datum_And_Atomic_Applicability()
    {
        var request = new FieldForwardConversionRequest();

        Assert.That(request.SourceGeographicReference, Is.EqualTo(FieldGeographicReference.ProjectionDatum));
        Assert.That(request.ProjectionApplicabilityPolicy, Is.EqualTo(FieldApplicabilityPolicy.RequireApplicable));
        Assert.That(request.Positions, Is.Empty);
    }

    [Test]
    public void Conversion_Response_Roundtrip_Preserves_Both_Geographic_Representations()
    {
        var response = new FieldCoordinateConversionResponse
        {
            FieldID = Guid.NewGuid(),
            ProjectionDefinition = new FieldCatalogReference { ID = Guid.NewGuid(), Name = "ETRS89 / UTM zone 32N" },
            ProjectionDatum = new FieldCatalogReference { ID = Guid.NewGuid(), Name = "ETRS89" },
            Wgs84Datum = new FieldCatalogReference { ID = Guid.NewGuid(), Name = "WGS 84" },
            ApiAxisConvention = "easting, then northing; SI metres",
            Positions =
            [
                new FieldCoordinateConversionPositionResult
                {
                    PositionIndex = 0,
                    ProjectionDatumGeographicCoordinate = new FieldGeographicCoordinate { Latitude = 1, Longitude = 0.1 },
                    Wgs84GeographicCoordinate = new FieldGeographicCoordinate { Latitude = 1.0001, Longitude = 0.1001 },
                    ProjectedCoordinate = new FieldProjectedCoordinate { Easting = 500000, Northing = 6650000 },
                    ProjectionDatumVerticalDepth = 1000,
                    Wgs84VerticalDepth = 1000
                }
            ]
        };

        string json = JsonSerializer.Serialize(response);
        FieldCoordinateConversionResponse? clone = JsonSerializer.Deserialize<FieldCoordinateConversionResponse>(json);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.Positions, Has.Count.EqualTo(1));
        Assert.That(clone.Positions[0].Wgs84GeographicCoordinate, Is.Not.Null);
        Assert.That(clone.Positions[0].ProjectedCoordinate.Easting, Is.EqualTo(500000));
    }
}
