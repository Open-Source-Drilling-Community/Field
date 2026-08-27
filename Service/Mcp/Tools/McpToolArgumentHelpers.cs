using System;
using System.Text.Json.Nodes;

namespace OSDC.Drilling.Field.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    public static JsonObject CreateEmptySchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false
    };

    public static JsonObject CreateGuidSchema(string key, string description)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [key] = new JsonObject
                {
                    ["type"] = "string",
                    ["format"] = "uuid",
                    ["description"] = description
                }
            },
            ["required"] = new JsonArray
            {
                key
            },
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateFieldSchema(bool includeId = false) =>
        WrapBody("field", CreateFieldObjectSchema(), includeId, "field.MetaInfo.ID");

    public static JsonObject CreateFieldForwardConversionSchema() => JsonNode.Parse("""
    {
      "type":"object",
      "properties":{
        "FieldID":{"type":"string","format":"uuid","description":"Existing Field UUID whose ProjectionDefinitionID selects the projected CRS."},
        "SourceGeographicReference":{"type":"string","enum":["ProjectionDatum","Wgs84"],"default":"ProjectionDatum"},
        "ProjectionApplicabilityPolicy":{"type":"string","enum":["RequireApplicable","AllowUnknown"],"default":"RequireApplicable"},
        "Transformation":{"$ref":"#/$defs/transformation"},
        "Positions":{"type":"array","minItems":1,"maxItems":1000,"items":{"type":"object","properties":{"Latitude":{"type":"number","minimum":-1.5707963267948966,"maximum":1.5707963267948966,"description":"SI radians, positive north."},"Longitude":{"type":"number","minimum":-3.141592653589793,"maximum":3.141592653589793,"description":"SI radians, positive east from the selected datum prime meridian."},"VerticalDepth":{"type":"number","description":"SI metres, positive downward."},"CoordinateEpochUtc":{"type":"string","format":"date-time"}},"required":["Latitude","Longitude","VerticalDepth"],"additionalProperties":false}}
      },
      "required":["FieldID","Positions"],
      "additionalProperties":false,
      "$defs":{"transformation":{"type":"object","properties":{"SelectionPolicy":{"type":"string","enum":["RequireUnambiguous","FirstAvailable","ExplicitPath"],"default":"RequireUnambiguous"},"TransformationPathIDs":{"type":"array","items":{"type":"string","format":"uuid"}},"SelectionToken":{"type":"string"},"ApplicabilityPolicy":{"type":"string","enum":["RequireApplicable","AllowUnknown"],"default":"RequireApplicable"},"DepthPolicy":{"type":"string","enum":["PreservePhysicalPoint","AllowUntransformedDepthFor2D"],"default":"AllowUntransformedDepthFor2D"}},"additionalProperties":false}}
    }
    """)!.AsObject();

    public static JsonObject CreateFieldInverseConversionSchema() => JsonNode.Parse("""
    {
      "type":"object",
      "properties":{
        "FieldID":{"type":"string","format":"uuid","description":"Existing Field UUID whose ProjectionDefinitionID selects the projected CRS."},
        "ProjectionApplicabilityPolicy":{"type":"string","enum":["RequireApplicable","AllowUnknown"],"default":"RequireApplicable"},
        "Transformation":{"$ref":"#/$defs/transformation"},
        "Positions":{"type":"array","minItems":1,"maxItems":1000,"items":{"type":"object","properties":{"Easting":{"type":"number","description":"Canonical projected easting in SI metres."},"Northing":{"type":"number","description":"Canonical projected northing in SI metres."},"VerticalDepth":{"type":"number","description":"SI metres, positive downward."},"CoordinateEpochUtc":{"type":"string","format":"date-time"}},"required":["Easting","Northing","VerticalDepth"],"additionalProperties":false}}
      },
      "required":["FieldID","Positions"],
      "additionalProperties":false,
      "$defs":{"transformation":{"type":"object","properties":{"SelectionPolicy":{"type":"string","enum":["RequireUnambiguous","FirstAvailable","ExplicitPath"],"default":"RequireUnambiguous"},"TransformationPathIDs":{"type":"array","items":{"type":"string","format":"uuid"}},"SelectionToken":{"type":"string"},"ApplicabilityPolicy":{"type":"string","enum":["RequireApplicable","AllowUnknown"],"default":"RequireApplicable"},"DepthPolicy":{"type":"string","enum":["PreservePhysicalPoint","AllowUntransformedDepthFor2D"],"default":"AllowUntransformedDepthFor2D"}},"additionalProperties":false}}
    }
    """)!.AsObject();

    public static JsonObject CreateFieldDelineationLineTypeSchema(bool includeId = false) =>
        WrapBody("fieldDelineationLineType", CreateNamedDefinitionSchema("field delineation line type"), includeId, "fieldDelineationLineType.MetaInfo.ID");

    public static JsonObject CreateFieldFeatureCategorySchema(bool includeId = false) =>
        WrapBody("fieldFeatureCategory", CreateCategorySchema("feature"), includeId, "fieldFeatureCategory.MetaInfo.ID");

    public static JsonObject CreateFieldIdentitySchema(bool includeId = false) =>
        WrapBody("fieldIdentity", CreateNamedDefinitionSchema("field identity"), includeId, "fieldIdentity.MetaInfo.ID");

    public static JsonObject CreateFieldMembershipCategorySchema(bool includeId = false) =>
        WrapBody("fieldMembershipCategory", CreateCategorySchema("membership"), includeId, "fieldMembershipCategory.MetaInfo.ID");

    private static JsonObject WrapBody(string key, JsonObject bodySchema, bool includeId, string bodyIdPath)
    {
        var properties = new JsonObject
        {
            [key] = bodySchema
        };
        var required = new JsonArray
        {
            key
        };

        if (includeId)
        {
            properties["id"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = $"Identifier of the stored record to update. It must equal {bodyIdPath}."
            };
            required.Add("id");
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    private static JsonObject CreateFieldObjectSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "Complete Field resource. MetaInfo.ID must be a caller-generated, non-empty UUID; ProjectionDefinitionID selects the EarthCartographicProjection definition used for stateless conversion.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("field"),
            ["Name"] = NullableString("Human-readable field name."),
            ["Description"] = NullableString("Human-readable field description."),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the field record was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification."),
            ["ProjectionDefinitionID"] = NullableUuid("Identifier of the EarthCartographicProjection projection definition used for field coordinate conversion."),
            ["ReferencePoint"] = CreatePointSchema("Optional field reference point using SI values and WGS84 references."),
            ["FieldFeatureAssignments"] = NullableArray("Feature options assigned to this field.", CreateCategoryAssignmentSchema("Feature")),
            ["FieldIdentityAssignments"] = NullableArray("Identity values assigned to this field.", CreateIdentityAssignmentSchema()),
            ["FieldMembershipAssignments"] = NullableArray("Membership options assigned to this field.", CreateCategoryAssignmentSchema("Membership")),
            ["DelineationLines"] = NullableArray("User-defined field delineation lines and any service-calculated margin boundaries.", CreateDelineationLineSchema())
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateNamedDefinitionSchema(string entity) => new()
    {
        ["type"] = "object",
        ["description"] = $"Complete {entity} definition. MetaInfo.ID must be a caller-generated, non-empty UUID.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema(entity),
            ["Name"] = NullableString($"Human-readable name of the {entity}."),
            ["CreationDate"] = NullableDateTime($"UTC or offset timestamp at which the {entity} was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification.")
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateCategorySchema(string kind) => new()
    {
        ["type"] = "object",
        ["description"] = $"Definition of a field {kind} category and its allowed options. MetaInfo.ID must be a caller-generated, non-empty UUID.",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema($"field {kind} category"),
            ["Name"] = NullableString($"Human-readable name of the field {kind} category."),
            ["IsExclusive"] = Boolean("True when at most one option from this category may be assigned at a time."),
            ["HasValidityPeriod"] = Boolean("True when assignments from this category use FromDate and ToDate validity boundaries."),
            ["Options"] = NullableArray("Allowed options in this category.", CreateOptionSchema()),
            ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the category was created."),
            ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification.")
        },
        ["required"] = new JsonArray { "MetaInfo" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateMetaInfoSchema(string resource) => new()
    {
        ["type"] = "object",
        ["description"] = $"Identity and optional HTTP location metadata for the {resource}.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid($"Non-empty unique identifier of the {resource}."),
            ["HttpHostName"] = NullableString($"Optional host name from which the {resource} can be retrieved."),
            ["HttpHostBasePath"] = NullableString($"Optional service base path from which the {resource} can be retrieved."),
            ["HttpEndPoint"] = NullableString($"Optional HTTP endpoint for this {resource} resource.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateCategoryAssignmentSchema(string kind) => new()
    {
        ["type"] = "object",
        ["description"] = $"Selection of one field {kind.ToLowerInvariant()} option, optionally constrained to a validity interval.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of this assignment."),
            [$"{kind}CategoryID"] = NullableUuid($"Identifier of the field {kind.ToLowerInvariant()} category."),
            [$"{kind}OptionID"] = NullableUuid("Identifier of the selected option within that category."),
            ["FromDate"] = NullableDateTime("First instant at which the assignment is valid."),
            ["ToDate"] = NullableDateTime("Last instant at which the assignment is valid.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateIdentityAssignmentSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "A field-specific value for a defined FieldIdentity.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of this assignment."),
            ["IdentityID"] = NullableUuid("Identifier of the FieldIdentity definition selected by this assignment."),
            ["Value"] = NullableString("Field-specific value for the selected identity.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateOptionSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "One selectable option within the category.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of the option within its category."),
            ["Name"] = NullableString("Human-readable option name.")
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateDelineationLineSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "A user-defined field boundary or delineation line. CalculatedBoundaryLines are derived by the service from Points and Margin.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of the delineation line."),
            ["DelineationLineTypeID"] = NullableUuid("Identifier of the standalone FieldDelineationLineType definition."),
            ["LineType"] = NullableString("Legacy user-defined line type retained for backward-compatible imports; prefer DelineationLineTypeID."),
            ["Name"] = NullableString("Human-readable delineation line name."),
            ["Description"] = NullableString("Human-readable delineation line description."),
            ["Margin"] = NullableNumber("Margin distance in meters (SI) used to calculate boundary lines."),
            ["TopDepth"] = NullableNumber("Optional top depth in meters (SI), referenced to WGS84."),
            ["BottomDepth"] = NullableNumber("Optional bottom depth in meters (SI), referenced to WGS84."),
            ["Points"] = NullableArray("Original line points using SI and WGS84 references.", CreatePointSchema("One original delineation point using SI and WGS84 references.")),
            ["CalculatedBoundaryLines"] = NullableArray("Service-calculated margin boundaries; normally omit these from create input.", CreateBoundaryLineSchema())
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreateBoundaryLineSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "A boundary line calculated by the service from a delineation line and its margin.",
        ["properties"] = new JsonObject
        {
            ["ID"] = Uuid("Stable identifier of the calculated boundary line."),
            ["IsInteriorBoundary"] = Boolean("True when this is the interior-side boundary of a closed input line."),
            ["IsClosed"] = Boolean("True when this calculated boundary line is closed."),
            ["Points"] = NullableArray("Calculated boundary points using SI and WGS84 references.", CreatePointSchema("One calculated boundary point using SI and WGS84 references."))
        },
        ["required"] = new JsonArray { "ID" },
        ["additionalProperties"] = false
    };

    private static JsonObject CreatePointSchema(string description) => new()
    {
        ["type"] = new JsonArray { "object", "null" },
        ["description"] = description + " Latitude/Longitude are radians; Riemannian coordinates and true vertical depth are meters.",
        ["properties"] = new JsonObject
        {
            ["X"] = NullableNumber("Meridian arc length from the equator in meters; synonymous with RiemannianNorth."),
            ["Y"] = NullableNumber("Arc length from Greenwich along the latitude parallel in meters; synonymous with RiemannianEast."),
            ["Z"] = NullableNumber("True vertical depth in meters, referenced to WGS84; synonymous with TVD."),
            ["RiemannianNorth"] = NullableNumber("Riemannian north coordinate in meters."),
            ["RiemannianEast"] = NullableNumber("Riemannian east coordinate in meters."),
            ["Latitude"] = NullableNumber("WGS84 latitude in radians."),
            ["Longitude"] = NullableNumber("WGS84 longitude in radians."),
            ["TVD"] = NullableNumber("True vertical depth in meters, referenced to WGS84.")
        },
        ["additionalProperties"] = false
    };

    private static JsonObject NullableArray(string description, JsonObject items) => new()
    {
        ["type"] = new JsonArray { "array", "null" },
        ["description"] = description,
        ["items"] = items
    };

    private static JsonObject ArraySchema(string description, JsonObject items, int minItems = 0) => new()
    {
        ["type"] = "array",
        ["description"] = description,
        ["items"] = items,
        ["minItems"] = minItems
    };

    private static JsonObject Uuid(string description) => new() { ["type"] = "string", ["format"] = "uuid", ["description"] = description };
    private static JsonObject NullableUuid(string description) => new() { ["type"] = new JsonArray { "string", "null" }, ["format"] = "uuid", ["description"] = description };
    private static JsonObject NullableString(string description) => new() { ["type"] = new JsonArray { "string", "null" }, ["description"] = description };
    private static JsonObject NullableDateTime(string description) => new() { ["type"] = new JsonArray { "string", "null" }, ["format"] = "date-time", ["description"] = description };
    private static JsonObject NullableNumber(string description) => new() { ["type"] = new JsonArray { "number", "null" }, ["description"] = description };
    private static JsonObject Integer(string description) => new() { ["type"] = "integer", ["description"] = description };
    private static JsonObject Boolean(string description) => new() { ["type"] = "boolean", ["description"] = description, ["default"] = false };

    public static bool TryParseGuid(JsonObject? arguments, string key, out Guid value, out JsonNode? error)
    {
        value = Guid.Empty;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        if (!Guid.TryParse(node.ToString(), out value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a valid UUID.");
            return false;
        }

        return true;
    }

    public static bool TryParseDouble(JsonObject? arguments, string key, out double value, out JsonNode? error)
    {
        value = 0d;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        try
        {
            value = node.GetValue<double>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a number.");
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a finite number.");
            return false;
        }

        return true;
    }
}
