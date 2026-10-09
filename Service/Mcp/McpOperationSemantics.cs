using System;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Service.Mcp;

internal static class McpOperationSemantics
{
    public static JsonNode Apply(string name, JsonNode schema)
    {
        string concept = name.StartsWith("field_feature_category_", StringComparison.Ordinal) ? Concepts.FeatureCategory
            : name.StartsWith("field_identity_", StringComparison.Ordinal) ? Concepts.IdentityDefinition
            : name.StartsWith("field_membership_category_", StringComparison.Ordinal) ? Concepts.MembershipCategory
            : name.StartsWith("field_delineation_line_type_", StringComparison.Ordinal) ? Concepts.FieldDelineationLine
            : name.Contains("convert_coordinates", StringComparison.Ordinal) ? Concepts.FieldCoordinateConversionRequest : Concepts.Field;
        schema[SemanticMetadata.ExtensionName] = SemanticMetadata.Create(concept, McpOperationRoleClassifier.Classify(name), assertionSource: "provider-mcp-operation");
        return schema;
    }
}

internal static class McpOperationRoleClassifier
{
    public static string Classify(string name) =>
        name.Contains("validate", StringComparison.Ordinal) || name.Contains("audit", StringComparison.Ordinal) || name.Contains("convert", StringComparison.Ordinal) || name.Contains("check", StringComparison.Ordinal) ? Concepts.StatelessEvaluation
        : name.Contains("batch_export", StringComparison.Ordinal) || name.Contains("get_all", StringComparison.Ordinal) || name.EndsWith("_search", StringComparison.Ordinal) ? Concepts.ResourceCollectionRetrieval
        : name.EndsWith("_get_by_id", StringComparison.Ordinal) ? Concepts.ResourceRetrieval
        : name.Contains("batch_restore", StringComparison.Ordinal) ? Concepts.ResourceOperation
        : name.EndsWith("_create", StringComparison.Ordinal) || name.EndsWith("_add", StringComparison.Ordinal) ? Concepts.ResourceCreation
        : name.EndsWith("_update_by_id", StringComparison.Ordinal) ? Concepts.ResourceReplacement
        : name.Contains("_update", StringComparison.Ordinal) || name.Contains("_patch", StringComparison.Ordinal) || name.Contains("_mutate", StringComparison.Ordinal) || name.Contains("_reorder", StringComparison.Ordinal) ? Concepts.ResourcePartialUpdate
        : name.Contains("_delete", StringComparison.Ordinal) ? Concepts.ResourceDeletion
        : Concepts.ResourceOperation;
}
