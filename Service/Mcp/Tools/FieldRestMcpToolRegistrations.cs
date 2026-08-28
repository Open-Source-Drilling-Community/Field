using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Field.Service.Controllers;
using OSDC.Drilling.Field.Service.Managers;
using FieldModel = OSDC.Drilling.Field.Model.Field;
using FieldForwardConversionRequestModel = OSDC.Drilling.Field.Model.FieldForwardConversionRequest;
using FieldInverseConversionRequestModel = OSDC.Drilling.Field.Model.FieldInverseConversionRequest;
using FieldDelineationLineTypeModel = OSDC.Drilling.Field.Model.FieldDelineationLineType;
using FieldFeatureCategoryModel = OSDC.Drilling.Field.Model.FieldFeatureCategory;
using FieldIdentityModel = OSDC.Drilling.Field.Model.FieldIdentity;
using FieldMembershipCategoryModel = OSDC.Drilling.Field.Model.FieldMembershipCategory;
using FieldBatchExportRequestModel = OSDC.Drilling.Field.Model.FieldBatchExportRequest;
using FieldBatchRestoreRequestModel = OSDC.Drilling.Field.Model.FieldBatchRestoreRequest;

namespace OSDC.Drilling.Field.Service.Mcp.Tools;

public static class FieldRestMcpToolRegistrations
{
    public static IServiceCollection AddFieldRestMcpTools(this IServiceCollection services)
    {
        AddFieldTools(services);
        AddFieldBatchTransferTools(services);
        AddFieldCoordinateConversionTools(services);
        AddFieldDelineationLineTypeTools(services);
        AddFieldFeatureCategoryTools(services);
        AddFieldIdentityTools(services);
        AddFieldMembershipCategoryTools(services);
        return services;
    }

    private static void AddFieldBatchTransferTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool(
            "field_batch_export",
            "Create a read-only schema-version-2 JSON backup of all stored fields or an explicitly ordered selection. The result includes the complete Field records and only the Field Feature, Field Membership, Field Identity, and Delineation Line Type definitions/options referenced by those fields. Projection definitions remain external UUID references. The selected operation is atomic: any missing or invalid field rejects the complete export.",
            McpToolArgumentHelpers.CreateFieldBatchExportSchema(),
            McpToolArgumentHelpers.CreateFieldBatchExportOutputSchema(),
            new McpToolBehavior("Export Fields with Catalog Dependencies", true, false, true, false),
            (sp, args, ct) => InvokeWithBodyResult<FieldBatchExportRequestModel, OSDC.Drilling.Field.Model.FieldBatchExportDocument>(args, "request", ct,
                request => FieldController(sp).BatchExportFields(request)));

        services.AddLegacyMcpTool(
            "field_batch_restore",
            "Validate and atomically restore a schema-version-2 Field backup document. Source catalog UUIDs are mapped to compatible local definitions by exact UUID or unique normalized name; MapOrCreateMissing may create missing local definitions/options with server-generated UUIDs. ReplaceExisting may replace fields with matching UUIDs. Catalog mapping, definition creation, reference rewriting, and all field writes share one transaction, so any validation, ambiguity, conflict, or storage failure changes nothing.",
            McpToolArgumentHelpers.CreateFieldBatchRestoreSchema(),
            McpToolArgumentHelpers.CreateFieldBatchRestoreOutputSchema(),
            new McpToolBehavior("Restore Fields and Catalog Dependencies", false, true, false, false),
            (sp, args, ct) => InvokeWithBodyResult<FieldBatchRestoreRequestModel, OSDC.Drilling.Field.Model.FieldBatchRestoreResponse>(args, "request", ct,
                request => FieldController(sp).BatchRestoreFields(request)));
    }

    private static void AddFieldTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool("field_get_all_ids", "List the UUID of every stored field without transferring complete records. Use these identifiers with field_get_by_id or in services whose resources reference a Field.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateIdsOutputSchema(), new("List Field UUIDs", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldId()));
        services.AddLegacyMcpTool("field_get_all_meta_info", "List identity and HTTP location metadata for every stored field without returning complete field data. Each result contains the field ID and may contain its host, base path, and endpoint.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateMetaInfoListOutputSchema(), new("List Field Metadata", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldMetaInfo()));
        services.AddLegacyMcpTool("field_get_by_id", "Retrieve one complete field by UUID, including its EarthCartographicProjection definition reference, WGS84 reference point, features, identities, memberships, and delineation lines. Returns 404 when absent and 400 for an empty UUID.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the field to retrieve."), McpToolArgumentHelpers.CreateResourceOutputSchema(McpToolArgumentHelpers.CreateFieldResourceSchema()), new("Get Field", true, false, true, false),
            (sp, args, ct) => InvokeById(args, ct, id => FieldController(sp).GetFieldById(id)));
        services.AddLegacyMcpTool("field_get_all", "Retrieve every stored field as a complete record, including assignments and delineation geometry. Use field_get_all_light, field_get_all_ids, or field_get_all_meta_info when full nested data is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateResourceListOutputSchema(McpToolArgumentHelpers.CreateFieldResourceSchema()), new("List Fields", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllField()));
        services.AddLegacyMcpTool("field_get_all_light", "Retrieve lightweight records for every field for discovery and selection workflows. These results omit the heavier nested assignment and delineation content returned by field_get_all.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateResourceListOutputSchema(McpToolArgumentHelpers.CreateFieldLightResourceSchema()), new("List Lightweight Fields", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldLight()));
        services.AddLegacyMcpTool("field_create", "Create and persist a complete field record. field.MetaInfo.ID must be a caller-generated, non-empty UUID that is not already stored. Field-owned catalog references are validated atomically. CreationDate and LastModificationDate are assigned by the server. Assign ProjectionDefinitionID to enable field-specific coordinate conversions.", McpToolArgumentHelpers.CreateFieldSchema(), McpToolArgumentHelpers.CreateResourceOutputSchema(McpToolArgumentHelpers.CreateFieldResourceSchema()), new("Create Field", false, false, false, false),
            (sp, args, ct) => InvokeWithBody<FieldModel>(args, "field", ct, data => FieldController(sp).PostField(data)));
        services.AddLegacyMcpTool("field_update_by_id", "Replace an existing field with the complete supplied record. The top-level id must equal field.MetaInfo.ID and expectedModifiedUtc must equal the latest server LastModificationDate. Field-owned catalog references are validated atomically. This is a full update, so include all data that should remain stored.", McpToolArgumentHelpers.CreateFieldSchema(includeId: true), McpToolArgumentHelpers.CreateResourceOutputSchema(McpToolArgumentHelpers.CreateFieldResourceSchema()), new("Update Field", false, true, true, false),
            (sp, args, ct) => InvokeWithIdAndBody<FieldModel>(args, "field", ct, (id, expected, data) => FieldController(sp).PutFieldById(id, expected, data)));
        services.AddLegacyMcpTool("field_delete_by_id", "Permanently delete one stored field by UUID. Confirm the target and consider clusters, wells, or other resources that may reference the field before calling. Returns 200 on success and 404 when absent.", McpToolArgumentHelpers.CreateGuidSchema("id", "Unique identifier of the field to delete."), McpToolArgumentHelpers.CreateStatusOnlyOutputSchema(), new("Delete Field", false, true, true, false),
            (sp, args, ct) => InvokeDelete(args, ct, id => FieldController(sp).DeleteFieldById(id)));
    }

    private static void AddFieldCoordinateConversionTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool("field_forward_convert_coordinates", "Synchronously convert an atomic ordered batch from geographic latitude/longitude to the selected Field's projected easting/northing. The Field supplies the immutable EarthCartographicProjection definition. Input may be in the projection datum or WGS 84; EarthGeodesy is called only when datum transformation is required. Angles are SI radians, distances are SI metres, and no request or result is persisted.", McpToolArgumentHelpers.CreateFieldForwardConversionSchema(), McpToolArgumentHelpers.CreateFieldCoordinateConversionOutputSchema(), new("Forward Field Coordinate Conversion", true, false, true, true),
            (sp, args, ct) => InvokeForwardConversion(sp, args, ct));
        services.AddLegacyMcpTool("field_inverse_convert_coordinates", "Synchronously convert an atomic ordered batch from the selected Field's projected easting/northing to geographic coordinates in the projection datum and, when a usable EarthGeodesy path exists, WGS 84. Distances are SI metres, angles are SI radians, ordering is preserved, and no request or result is persisted.", McpToolArgumentHelpers.CreateFieldInverseConversionSchema(), McpToolArgumentHelpers.CreateFieldCoordinateConversionOutputSchema(), new("Inverse Field Coordinate Conversion", true, false, true, true),
            (sp, args, ct) => InvokeInverseConversion(sp, args, ct));
    }

    private static void AddFieldDelineationLineTypeTools(IServiceCollection services)
    {
        AddCrudTools<FieldDelineationLineTypeModel>(
            services,
            "field_delineation_line_type",
            "fieldDelineationLineType",
            "field delineation line type",
            "a reusable classification referenced by user-defined field delineation lines",
            McpToolArgumentHelpers.CreateFieldDelineationLineTypeSchema,
            McpToolArgumentHelpers.CreateFieldDelineationLineTypeResourceSchema,
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineTypeId(),
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineTypeMetaInfo(),
            (sp, id) => FieldDelineationLineTypeController(sp).GetFieldDelineationLineTypeById(id),
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineType(),
            (sp, data) => FieldDelineationLineTypeController(sp).PostFieldDelineationLineType(data),
            (sp, id, expected, data) => FieldDelineationLineTypeController(sp).PutFieldDelineationLineTypeById(id, expected, data),
            (sp, id) => FieldDelineationLineTypeController(sp).DeleteFieldDelineationLineTypeById(id));
    }

    private static void AddFieldFeatureCategoryTools(IServiceCollection services)
    {
        AddCrudTools<FieldFeatureCategoryModel>(
            services,
            "field_feature_category",
            "fieldFeatureCategory",
            "field feature category",
            "a definition of allowed feature options that can be assigned to fields",
            McpToolArgumentHelpers.CreateFieldFeatureCategorySchema,
            McpToolArgumentHelpers.CreateFieldFeatureCategoryResourceSchema,
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategoryId(),
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategoryMetaInfo(),
            (sp, id) => FieldFeatureCategoryController(sp).GetFieldFeatureCategoryById(id),
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategory(),
            (sp, data) => FieldFeatureCategoryController(sp).PostFieldFeatureCategory(data),
            (sp, id, expected, data) => FieldFeatureCategoryController(sp).PutFieldFeatureCategoryById(id, expected, data),
            (sp, id) => FieldFeatureCategoryController(sp).DeleteFieldFeatureCategoryById(id));
    }

    private static void AddFieldIdentityTools(IServiceCollection services)
    {
        AddCrudTools<FieldIdentityModel>(
            services,
            "field_identity",
            "fieldIdentity",
            "field identity",
            "a symbolic identity definition whose values can be assigned to individual fields",
            McpToolArgumentHelpers.CreateFieldIdentitySchema,
            McpToolArgumentHelpers.CreateFieldIdentityResourceSchema,
            sp => FieldIdentityController(sp).GetAllFieldIdentityId(),
            sp => FieldIdentityController(sp).GetAllFieldIdentityMetaInfo(),
            (sp, id) => FieldIdentityController(sp).GetFieldIdentityById(id),
            sp => FieldIdentityController(sp).GetAllFieldIdentity(),
            (sp, data) => FieldIdentityController(sp).PostFieldIdentity(data),
            (sp, id, expected, data) => FieldIdentityController(sp).PutFieldIdentityById(id, expected, data),
            (sp, id) => FieldIdentityController(sp).DeleteFieldIdentityById(id));
    }

    private static void AddFieldMembershipCategoryTools(IServiceCollection services)
    {
        AddCrudTools<FieldMembershipCategoryModel>(
            services,
            "field_membership_category",
            "fieldMembershipCategory",
            "field membership category",
            "a definition of allowed membership options that can be assigned to fields",
            McpToolArgumentHelpers.CreateFieldMembershipCategorySchema,
            McpToolArgumentHelpers.CreateFieldMembershipCategoryResourceSchema,
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategoryId(),
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategoryMetaInfo(),
            (sp, id) => FieldMembershipCategoryController(sp).GetFieldMembershipCategoryById(id),
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategory(),
            (sp, data) => FieldMembershipCategoryController(sp).PostFieldMembershipCategory(data),
            (sp, id, expected, data) => FieldMembershipCategoryController(sp).PutFieldMembershipCategoryById(id, expected, data),
            (sp, id) => FieldMembershipCategoryController(sp).DeleteFieldMembershipCategoryById(id));
    }

    private static void AddCrudTools<TModel>(
        IServiceCollection services,
        string prefix,
        string bodyName,
        string entityName,
        string entityPurpose,
        Func<bool, JsonObject> schemaFactory,
        Func<JsonObject> resourceSchemaFactory,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<Guid>>> getAllIds,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>>> getAllMetaInfo,
        Func<IServiceProvider, Guid, ActionResult<TModel?>> getById,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<TModel?>>> getAll,
        Func<IServiceProvider, TModel?, ActionResult> create,
        Func<IServiceProvider, Guid, DateTimeOffset, TModel?, ActionResult> update,
        Func<IServiceProvider, Guid, ActionResult> delete)
    {
        string optionRemovalRule = prefix is "field_feature_category" or "field_membership_category"
            ? " Removing an option still referenced by a Field is rejected with reference_conflict."
            : string.Empty;
        services.AddLegacyMcpTool($"{prefix}_get_all_ids", $"List the UUID of every stored {entityName} without transferring complete records. These IDs identify {entityPurpose} and can be passed to {prefix}_get_by_id.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateIdsOutputSchema(), new($"List {entityName} UUIDs", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => getAllIds(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_all_meta_info", $"List identity and optional HTTP location metadata for every stored {entityName} without returning complete definitions. Use this for resource discovery when full content is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateMetaInfoListOutputSchema(), new($"List {entityName} Metadata", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => getAllMetaInfo(sp)));
        services.AddLegacyMcpTool($"{prefix}_get_by_id", $"Retrieve one complete {entityName} by UUID. The record represents {entityPurpose}. Returns status 404 when no matching record exists and 400 for an empty UUID.", McpToolArgumentHelpers.CreateGuidSchema("id", $"Unique identifier of the {entityName} to retrieve."), McpToolArgumentHelpers.CreateResourceOutputSchema(resourceSchemaFactory()), new($"Get {entityName}", true, false, true, false),
            (sp, args, ct) => InvokeById(args, ct, id => getById(sp, id)));
        services.AddLegacyMcpTool($"{prefix}_get_all", $"Retrieve every stored {entityName} as a complete definition. Each result represents {entityPurpose}; use the ID or metadata listing tools when complete content is unnecessary.", McpToolArgumentHelpers.CreateEmptySchema(), McpToolArgumentHelpers.CreateResourceListOutputSchema(resourceSchemaFactory()), new($"List {entityName}", true, false, true, false),
            (sp, _, ct) => Invoke(ct, () => getAll(sp)));
        services.AddLegacyMcpTool($"{prefix}_create", $"Create and persist {entityPurpose}. Supply the complete {bodyName} object; {bodyName}.MetaInfo.ID must be a caller-generated, non-empty UUID that is not already stored. CreationDate and LastModificationDate are assigned by the server.", schemaFactory(false), McpToolArgumentHelpers.CreateResourceOutputSchema(resourceSchemaFactory()), new($"Create {entityName}", false, false, false, false),
            (sp, args, ct) => InvokeWithBody<TModel>(args, bodyName, ct, data => create(sp, data)));
        services.AddLegacyMcpTool($"{prefix}_update_by_id", $"Replace an existing {entityName} with the complete supplied definition. The top-level id must equal {bodyName}.MetaInfo.ID and expectedModifiedUtc must equal the latest server LastModificationDate. This is a full update.{optionRemovalRule}", schemaFactory(true), McpToolArgumentHelpers.CreateResourceOutputSchema(resourceSchemaFactory()), new($"Update {entityName}", false, true, true, false),
            (sp, args, ct) => InvokeWithIdAndBody<TModel>(args, bodyName, ct, (id, expected, data) => update(sp, id, expected, data)));
        services.AddLegacyMcpTool($"{prefix}_delete_by_id", $"Permanently delete one stored {entityName} by UUID. The operation is rejected with reference_conflict while any Field references the definition.", McpToolArgumentHelpers.CreateGuidSchema("id", $"Unique identifier of the {entityName} to delete."), McpToolArgumentHelpers.CreateStatusOnlyOutputSchema(), new($"Delete {entityName}", false, true, true, false),
            (sp, args, ct) => InvokeDelete(args, ct, id => delete(sp, id)));
    }

    private static Task<JsonNode?> Invoke<T>(CancellationToken cancellationToken, Func<ActionResult<T>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action()));
    }

    private static Task<JsonNode?> Invoke(CancellationToken cancellationToken, Func<ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action()));
    }

    private static Task<JsonNode?> InvokeById<T>(JsonObject? arguments, CancellationToken cancellationToken, Func<Guid, ActionResult<T>> action)
    {
        return InvokeByGuidArgument(arguments, "id", cancellationToken, action);
    }

    private static Task<JsonNode?> InvokeByGuidArgument<T>(JsonObject? arguments, string argumentName, CancellationToken cancellationToken, Func<Guid, ActionResult<T>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, argumentName, out Guid id, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)));
    }

    private static Task<JsonNode?> InvokeDelete(JsonObject? arguments, CancellationToken cancellationToken, Func<Guid, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id)));
    }

    private static Task<JsonNode?> InvokeWithBody<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<TModel?, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? error))
        {
            return Task.FromResult<JsonNode?>(error);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data)));
    }

    private static Task<JsonNode?> InvokeWithBodyResult<TModel, TResult>(JsonObject? arguments, string bodyName,
        CancellationToken cancellationToken, Func<TModel?, ActionResult<TResult>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? error))
            return Task.FromResult<JsonNode?>(error);
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(data)));
    }

    private static Task<JsonNode?> InvokeWithIdAndBody<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<Guid, DateTimeOffset, TModel?, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? idError))
        {
            return Task.FromResult<JsonNode?>(idError);
        }
        if (!McpToolArgumentHelpers.TryParseDateTimeOffset(arguments, "expectedModifiedUtc", out DateTimeOffset expectedModifiedUtc, out JsonNode? timestampError))
        {
            return Task.FromResult<JsonNode?>(timestampError);
        }
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? dataError))
        {
            return Task.FromResult<JsonNode?>(dataError);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, expectedModifiedUtc, data)));
    }

    private static async Task<JsonNode?> InvokeWithBodyAsync<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<TModel?, Task<ActionResult>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? error))
        {
            return error;
        }
        return McpActionResultConverter.FromActionResult(await action(data).ConfigureAwait(false));
    }

    private static async Task<JsonNode?> InvokeWithIdAndBodyAsync<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<Guid, TModel?, Task<ActionResult>> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? idError))
        {
            return idError;
        }
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? dataError))
        {
            return dataError;
        }
        return McpActionResultConverter.FromActionResult(await action(id, data).ConfigureAwait(false));
    }

    private static async Task<JsonNode?> InvokeForwardConversion(IServiceProvider serviceProvider, JsonObject? arguments, CancellationToken cancellationToken)
    {
        if (!TryDeserializeDirect(arguments, out FieldForwardConversionRequestModel? request, out JsonNode? error)) return error;
        ActionResult<OSDC.Drilling.Field.Model.FieldCoordinateConversionResponse> result =
            await FieldCoordinateConversionController(serviceProvider).Forward(request!, cancellationToken).ConfigureAwait(false);
        return McpActionResultConverter.FromActionResult(result);
    }

    private static async Task<JsonNode?> InvokeInverseConversion(IServiceProvider serviceProvider, JsonObject? arguments, CancellationToken cancellationToken)
    {
        if (!TryDeserializeDirect(arguments, out FieldInverseConversionRequestModel? request, out JsonNode? error)) return error;
        ActionResult<OSDC.Drilling.Field.Model.FieldCoordinateConversionResponse> result =
            await FieldCoordinateConversionController(serviceProvider).Inverse(request!, cancellationToken).ConfigureAwait(false);
        return McpActionResultConverter.FromActionResult(result);
    }

    private static bool TryDeserializeDirect<TModel>(JsonObject? arguments, out TModel? data, out JsonNode? error)
        where TModel : class
    {
        data = default;
        error = null;
        try
        {
            data = arguments?.Deserialize<TModel>(JsonSettings.Options);
            if (data == null) throw new JsonException();
            return true;
        }
        catch (JsonException)
        {
            error = McpToolResponses.CreateValidationError("The coordinate-conversion arguments could not be deserialized.");
            return false;
        }
    }

    private static bool TryDeserialize<TModel>(JsonObject? arguments, string bodyName, out TModel? data, out JsonNode? error)
    {
        data = default;
        error = null;

        if (arguments?[bodyName] is not JsonNode node)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' is required.");
            return false;
        }

        try
        {
            data = node.Deserialize<TModel>(JsonSettings.Options);
            if (data is null)
            {
                throw new InvalidOperationException();
            }
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{bodyName}' could not be deserialized.");
            return false;
        }
    }

    private static FieldController FieldController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldCoordinateConversionController FieldCoordinateConversionController(IServiceProvider sp) =>
        new(sp.GetRequiredService<FieldCoordinateConversionService>());

    private static FieldDelineationLineTypeController FieldDelineationLineTypeController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldDelineationLineTypeManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldFeatureCategoryController FieldFeatureCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldFeatureCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldIdentityController FieldIdentityController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldIdentityManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldMembershipCategoryController FieldMembershipCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldMembershipCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

}
