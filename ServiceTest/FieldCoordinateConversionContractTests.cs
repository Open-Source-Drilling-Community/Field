using System.Text.Json;
using OSDC.Drilling.Field.ModelShared;

namespace OSDC.Drilling.Field.ServiceTest;

public class FieldCoordinateConversionContractTests
{
    [Test]
    public void Generated_client_exposes_only_stateless_conversion_operations()
    {
        string[] methodNames = typeof(Client).GetMethods().Select(method => method.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(methodNames, Does.Contain("ForwardFieldCoordinatesAsync"));
            Assert.That(methodNames, Does.Contain("InverseFieldCoordinatesAsync"));
            Assert.That(methodNames.Any(name => name.Contains("FieldCartographicConversionSet", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void Forward_request_serializes_explicit_field_and_geographic_reference()
    {
        Guid fieldId = Guid.NewGuid();
        var request = new FieldForwardConversionRequest
        {
            FieldID = fieldId,
            SourceGeographicReference = FieldGeographicReference.Wgs84,
            ProjectionApplicabilityPolicy = FieldApplicabilityPolicy.AllowUnknown,
            Positions = [new FieldForwardConversionPosition { Latitude = 1, Longitude = 0.1, VerticalDepth = 2 }]
        };

        string json = JsonSerializer.Serialize(request);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain($"\"FieldID\":\"{fieldId}\""));
            Assert.That(json, Does.Contain("\"SourceGeographicReference\":\"Wgs84\""));
            Assert.That(json, Does.Contain("\"Positions\""));
        });
    }
}
