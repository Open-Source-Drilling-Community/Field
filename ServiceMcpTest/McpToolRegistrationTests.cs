using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using NORCE.Drilling.Field.Service.Mcp;
using NORCE.Drilling.Field.Service.Mcp.Tools;
using NUnit.Framework;

namespace NORCE.Drilling.Field.ServiceMcpTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    private ServiceProvider _provider = null!;
    private IReadOnlyDictionary<string, IMcpTool> _tools = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddFieldRestMcpTools();
        _provider = services.BuildServiceProvider();
        _tools = _provider.GetServices<IMcpTool>().ToDictionary(tool => tool.Name);
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public void Rest_tools_have_detailed_descriptions_and_explicit_schemas()
    {
        Assert.That(_tools.Keys, Is.Unique);
        Assert.That(_tools.Keys.All(name => !name.Contains('.')), Is.True);
        foreach (IMcpTool tool in _tools.Values.Where(tool => tool.Name != "ping"))
        {
            Assert.That(tool.Description, Has.Length.GreaterThan(100), tool.Name);
            Assert.That(tool.InputSchema, Is.TypeOf<JsonObject>(), tool.Name);
            Assert.That(tool.OutputSchema, Is.TypeOf<JsonObject>(), tool.Name);
            Assert.That(tool.Behavior.Title, Is.Not.Empty, tool.Name);
        }
    }

    [Test]
    public void Conversion_tool_annotations_are_read_only_idempotent_and_open_world()
    {
        foreach (string name in new[] { "field_forward_convert_coordinates", "field_inverse_convert_coordinates" })
        {
            McpToolBehavior behavior = _tools[name].Behavior;
            Assert.Multiple(() =>
            {
                Assert.That(behavior.ReadOnlyHint, Is.True, name);
                Assert.That(behavior.DestructiveHint, Is.False, name);
                Assert.That(behavior.IdempotentHint, Is.True, name);
                Assert.That(behavior.OpenWorldHint, Is.True, name);
            });
        }
    }

    [Test]
    public void Conversion_tools_are_stateless_and_replace_the_persistent_case_lifecycle()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_tools, Does.ContainKey("field_forward_convert_coordinates"));
            Assert.That(_tools, Does.ContainKey("field_inverse_convert_coordinates"));
            Assert.That(_tools.Keys.Any(name => name.Contains("cartographic_conversion_set", StringComparison.Ordinal)), Is.False);
            Assert.That(_tools["field_forward_convert_coordinates"].Description, Does.Contain("no request or result is persisted"));
            Assert.That(_tools["field_inverse_convert_coordinates"].Description, Does.Contain("atomic ordered batch"));
        });
    }

    [Test]
    public void Forward_conversion_schema_describes_batch_units_and_reference_datum()
    {
        JsonObject root = RequireObject(_tools["field_forward_convert_coordinates"].InputSchema);
        JsonObject positions = Property(root, "Positions");
        JsonObject coordinate = RequireObject(positions["items"]);
        Assert.Multiple(() =>
        {
            Assert.That(RequiredNames(root), Is.EquivalentTo(new[] { "FieldID", "Positions" }));
            Assert.That(Property(root, "FieldID")["type"]?.GetValue<string>(), Is.EqualTo("string"));
            Assert.That(Property(root, "SourceGeographicReference")["enum"]!.AsArray().Select(n => n!.GetValue<string>()), Is.EquivalentTo(new[] { "ProjectionDatum", "Wgs84" }));
            Assert.That(positions["minItems"]?.GetValue<int>(), Is.EqualTo(1));
            Assert.That(positions["maxItems"]?.GetValue<int>(), Is.EqualTo(1000));
            Assert.That(Property(coordinate, "Latitude")["description"]?.GetValue<string>(), Does.Contain("radians"));
            Assert.That(Property(coordinate, "VerticalDepth")["description"]?.GetValue<string>(), Does.Contain("metres"));
        });
    }

    [Test]
    public void Inverse_conversion_schema_uses_unambiguous_projected_axis_order()
    {
        JsonObject root = RequireObject(_tools["field_inverse_convert_coordinates"].InputSchema);
        JsonObject coordinate = RequireObject(Property(root, "Positions")["items"]);
        Assert.That(RequiredNames(coordinate), Is.EquivalentTo(new[] { "Easting", "Northing", "VerticalDepth" }));
        Assert.That(Property(coordinate, "Easting")["description"]?.GetValue<string>(), Does.Contain("metres"));
    }

    [TestCase("field_get_all_ids")]
    [TestCase("field_get_all")]
    [TestCase("field_identity_get_all")]
    public void Parameterless_tools_publish_an_explicit_empty_schema(string toolName)
    {
        JsonObject schema = RequireObject(_tools[toolName].InputSchema);
        Assert.That(schema["type"]?.GetValue<string>(), Is.EqualTo("object"));
        Assert.That(schema["additionalProperties"]?.GetValue<bool>(), Is.False);
    }

    private static JsonObject RequireObject(JsonNode? node)
    {
        Assert.That(node, Is.TypeOf<JsonObject>());
        return (JsonObject)node!;
    }

    private static JsonObject Property(JsonObject schema, string name) =>
        RequireObject(RequireObject(schema["properties"])[name]);

    private static string[] RequiredNames(JsonObject schema)
    {
        Assert.That(schema["required"], Is.TypeOf<JsonArray>());
        return ((JsonArray)schema["required"]!).Select(node => node!.GetValue<string>()).ToArray();
    }
}
