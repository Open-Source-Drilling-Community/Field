using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using NUnit.Framework;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.Math;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.Service;
using OSDC.Drilling.Field.Service.Mcp;
using OSDC.Drilling.Field.Service.Mcp.Tools;
using Swashbuckle.AspNetCore.SwaggerGen;
using Model = OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.SemanticTests;

public class SemanticContractTests
{
    private const string Extension = SemanticMetadata.ExtensionName;

    private static JsonObject Rest(Type type)
    {
        var options = new SchemaGeneratorOptions { SchemaIdSelector = t => t.FullName! };
        options.SchemaFilters.Add(new SemanticSchemaFilter());
        var generator = new SchemaGenerator(options, new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
        var repository = new SchemaRepository();
        generator.GenerateSchema(type, repository);
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        repository.Schemas[type.FullName!].SerializeAsV3(writer);
        writer.Flush();
        return JsonNode.Parse(text.ToString())!.AsObject();
    }

    private static JsonObject Mcp(string name, bool output = true)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddFieldRestMcpTools();
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<IMcpTool>().Single(t => t.Name == name);
        return (output ? tool.OutputSchema : tool.InputSchema).AsObject();
    }

    [Test]
    public void ResourceAndInheritedClassificationsPublishCuratedCatalogue()
    {
        var rest = Rest(typeof(Model.Field));
        var mcp = Mcp("field_get_by_id")["properties"]!["data"]!;
        Assert.That(rest[Extension]!["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.7.0"));
        Assert.That(JsonNode.DeepEquals(rest[Extension], mcp[Extension]), Is.True);
        var category = Rest(typeof(Model.FieldFeatureCategory));
        Assert.That(category[Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.FeatureCategory));
        Assert.That(category["properties"]!["IsExclusive"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.CategoryExclusivity));
        var assignment = Rest(typeof(Model.FieldFeatureAssignment));
        Assert.That(assignment["properties"]!["FromDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityStart));
        Assert.That(assignment["properties"]!["ToDate"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ValidityEnd));
        Assert.That(assignment["properties"]!["FeatureOptionID"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.ResourceIdentifier));
    }

    [Test]
    public void ArcCoordinatesAreNotProjectionCoordinatesAndReferenceRoleSurvivesRecursion()
    {
        var rest = Rest(typeof(Point3DGlobalCoordinates));
        var mcp = Mcp("field_get_by_id")["properties"]!["data"]!["properties"]!["ReferencePoint"]!;
        Assert.That(mcp[Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.ReferenceLocation));
        foreach (string name in new[] { "X", "Y", "Z", "RiemannianNorth", "RiemannianEast", "Latitude", "Longitude", "TVD" })
            Assert.That(JsonNode.DeepEquals(rest["properties"]![name]![Extension], mcp["properties"]![name]![Extension]), Is.True, name);
        Assert.That(rest["properties"]!["X"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.RiemannianNorth));
        Assert.That(rest["properties"]!["Y"]![Extension]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.RiemannianEast));
        Assert.That(rest["properties"]!["Z"]![Extension]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("DepthDrilling"));
    }

    [Test]
    public void EveryPublishedBindingResolvesToReviewedVocabulary()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddFieldRestMcpTools();
        using var provider = services.BuildServiceProvider();
        int count = 0;
        void Check(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["catalogue"] != null)
                {
                    if (obj["concept"] != null)
                    {
                        count++;
                        Assert.That(obj["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.7.0"));
                        Assert.That(obj["curationStatus"]!.GetValue<string>(), Is.EqualTo("Reviewed"));
                        Assert.That(SemanticCatalogue.Default.Get(obj["concept"]!.GetValue<string>()).Status, Is.EqualTo(CurationStatus.Reviewed));
                    }
                }
                foreach (var child in obj) Check(child.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) Check(child);
        }
        foreach (var tool in provider.GetServices<IMcpTool>()) { Check(tool.InputSchema); Check(tool.OutputSchema); }
        Assert.That(count, Is.GreaterThan(100));
    }

    [Test]
    public void DelineationUnitsAndConversionReferenceContextsAreExplicit()
    {
        var line = Rest(typeof(Model.FieldDelineationLine))["properties"]!;
        Assert.That(line["Margin"]![Extension]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("LengthStandard"));
        Assert.That(line["TopDepth"]![Extension]!["role"]!.GetValue<string>(), Is.EqualTo(Concepts.TopDepthBoundary));
        Assert.That(line["BottomDepth"]![Extension]!["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.Wgs84));
        var conversion = Rest(typeof(Model.FieldCoordinateConversionPositionResult))["properties"]!;
        Assert.That(conversion["ProjectionDatumVerticalDepth"]![Extension]!["reference"], Is.Null, "Projection datum is selected dynamically");
        Assert.That(conversion["Wgs84VerticalDepth"]![Extension]!["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.Wgs84));
        Assert.That(conversion["GridConvergence"]![Extension]!["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.GridConvergenceTrueToGridClockwise));
        var geographic = Rest(typeof(Model.FieldGeographicCoordinate))["properties"]!;
        Assert.That(geographic["Longitude"]![Extension]!["siUnit"]!.GetValue<string>(), Is.EqualTo("rad"));
        Assert.That(geographic["Longitude"]![Extension]!["reference"], Is.Null);
    }
}
