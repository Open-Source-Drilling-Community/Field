using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Field.Service.Controllers;
using NORCE.Drilling.Field.Service.Managers;
using FieldModel = NORCE.Drilling.Field.Model.Field;
using FieldCartographicConversionSetModel = NORCE.Drilling.Field.Model.FieldCartographicConversionSet;
using FieldDelineationLineTypeModel = NORCE.Drilling.Field.Model.FieldDelineationLineType;
using FieldFeatureCategoryModel = NORCE.Drilling.Field.Model.FieldFeatureCategory;
using FieldIdentityModel = NORCE.Drilling.Field.Model.FieldIdentity;
using FieldMembershipCategoryModel = NORCE.Drilling.Field.Model.FieldMembershipCategory;

namespace NORCE.Drilling.Field.Service.Mcp.Tools;

internal static class FieldRestMcpToolRegistrations
{
    public static IServiceCollection AddFieldRestMcpTools(this IServiceCollection services)
    {
        AddFieldTools(services);
        AddFieldCartographicConversionSetTools(services);
        AddFieldDelineationLineTypeTools(services);
        AddFieldFeatureCategoryTools(services);
        AddFieldIdentityTools(services);
        AddFieldMembershipCategoryTools(services);
        AddUsageStatisticsTool(services);
        return services;
    }

    private static void AddFieldTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool("field.get_all_ids", "Retrieve all field identifiers.", null,
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldId()));
        services.AddLegacyMcpTool("field.get_all_meta_info", "Retrieve metadata for all fields.", null,
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldMetaInfo()));
        services.AddLegacyMcpTool("field.get_by_id", "Retrieve a field by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeById(args, ct, id => FieldController(sp).GetFieldById(id)));
        services.AddLegacyMcpTool("field.get_all", "Retrieve all fields with full data.", null,
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllField()));
        services.AddLegacyMcpTool("field.get_all_light", "Retrieve all fields as lightweight records.", null,
            (sp, _, ct) => Invoke(ct, () => FieldController(sp).GetAllFieldLight()));
        services.AddLegacyMcpTool("field.create", "Create a field.", McpToolArgumentHelpers.CreateObjectSchema("field"),
            (sp, args, ct) => InvokeWithBody<FieldModel>(args, "field", ct, data => FieldController(sp).PostField(data)));
        services.AddLegacyMcpTool("field.update_by_id", "Update an existing field identified by id.", McpToolArgumentHelpers.CreateObjectSchema("field", includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<FieldModel>(args, "field", ct, (id, data) => FieldController(sp).PutFieldById(id, data)));
        services.AddLegacyMcpTool("field.delete_by_id", "Delete a field by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeDelete(args, ct, id => FieldController(sp).DeleteFieldById(id)));
    }

    private static void AddFieldCartographicConversionSetTools(IServiceCollection services)
    {
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_all_ids", "Retrieve all field cartographic conversion set identifiers.", null,
            (sp, _, ct) => Invoke(ct, () => FieldCartographicConversionSetController(sp).GetAllFieldCartographicConversionSetId()));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_all_meta_info", "Retrieve metadata for all field cartographic conversion sets.", null,
            (sp, _, ct) => Invoke(ct, () => FieldCartographicConversionSetController(sp).GetAllFieldCartographicConversionSetMetaInfo()));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_by_id", "Retrieve a field cartographic conversion set by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeById(args, ct, id => FieldCartographicConversionSetController(sp).GetFieldCartographicConversionSetById(id)));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_all_by_field_id", "Retrieve field cartographic conversion sets for a field identifier.", McpToolArgumentHelpers.CreateGuidSchema("fieldId"),
            (sp, args, ct) => InvokeByGuidArgument(args, "fieldId", ct, fieldId => FieldCartographicConversionSetController(sp).GetAllFieldCartographicConversionSetByFieldId(fieldId)));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_all_light", "Retrieve all field cartographic conversion sets as lightweight records.", null,
            (sp, _, ct) => Invoke(ct, () => FieldCartographicConversionSetController(sp).GetAllFieldCartographicConversionSetLight()));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.get_all", "Retrieve all field cartographic conversion sets with full data.", null,
            (sp, _, ct) => Invoke(ct, () => FieldCartographicConversionSetController(sp).GetAllFieldCartographicConversionSet()));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.create", "Calculate and create a field cartographic conversion set.", McpToolArgumentHelpers.CreateObjectSchema("fieldCartographicConversionSet"),
            (sp, args, ct) => InvokeWithBodyAsync<FieldCartographicConversionSetModel>(args, "fieldCartographicConversionSet", ct, data => FieldCartographicConversionSetController(sp).PostFieldCartographicConversionSet(data)));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.update_by_id", "Calculate and update an existing field cartographic conversion set identified by id.", McpToolArgumentHelpers.CreateObjectSchema("fieldCartographicConversionSet", includeId: true),
            (sp, args, ct) => InvokeWithIdAndBodyAsync<FieldCartographicConversionSetModel>(args, "fieldCartographicConversionSet", ct, (id, data) => FieldCartographicConversionSetController(sp).PutFieldCartographicConversionSetById(id, data)));
        services.AddLegacyMcpTool("field_cartographic_conversion_set.delete_by_id", "Delete a field cartographic conversion set by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeDelete(args, ct, id => FieldCartographicConversionSetController(sp).DeleteFieldCartographicConversionSetById(id)));
    }

    private static void AddFieldDelineationLineTypeTools(IServiceCollection services)
    {
        AddCrudTools<FieldDelineationLineTypeModel>(
            services,
            "field_delineation_line_type",
            "fieldDelineationLineType",
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineTypeId(),
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineTypeMetaInfo(),
            (sp, id) => FieldDelineationLineTypeController(sp).GetFieldDelineationLineTypeById(id),
            sp => FieldDelineationLineTypeController(sp).GetAllFieldDelineationLineType(),
            (sp, data) => FieldDelineationLineTypeController(sp).PostFieldDelineationLineType(data),
            (sp, id, data) => FieldDelineationLineTypeController(sp).PutFieldDelineationLineTypeById(id, data),
            (sp, id) => FieldDelineationLineTypeController(sp).DeleteFieldDelineationLineTypeById(id));
    }

    private static void AddFieldFeatureCategoryTools(IServiceCollection services)
    {
        AddCrudTools<FieldFeatureCategoryModel>(
            services,
            "field_feature_category",
            "fieldFeatureCategory",
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategoryId(),
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategoryMetaInfo(),
            (sp, id) => FieldFeatureCategoryController(sp).GetFieldFeatureCategoryById(id),
            sp => FieldFeatureCategoryController(sp).GetAllFieldFeatureCategory(),
            (sp, data) => FieldFeatureCategoryController(sp).PostFieldFeatureCategory(data),
            (sp, id, data) => FieldFeatureCategoryController(sp).PutFieldFeatureCategoryById(id, data),
            (sp, id) => FieldFeatureCategoryController(sp).DeleteFieldFeatureCategoryById(id));
    }

    private static void AddFieldIdentityTools(IServiceCollection services)
    {
        AddCrudTools<FieldIdentityModel>(
            services,
            "field_identity",
            "fieldIdentity",
            sp => FieldIdentityController(sp).GetAllFieldIdentityId(),
            sp => FieldIdentityController(sp).GetAllFieldIdentityMetaInfo(),
            (sp, id) => FieldIdentityController(sp).GetFieldIdentityById(id),
            sp => FieldIdentityController(sp).GetAllFieldIdentity(),
            (sp, data) => FieldIdentityController(sp).PostFieldIdentity(data),
            (sp, id, data) => FieldIdentityController(sp).PutFieldIdentityById(id, data),
            (sp, id) => FieldIdentityController(sp).DeleteFieldIdentityById(id));
    }

    private static void AddFieldMembershipCategoryTools(IServiceCollection services)
    {
        AddCrudTools<FieldMembershipCategoryModel>(
            services,
            "field_membership_category",
            "fieldMembershipCategory",
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategoryId(),
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategoryMetaInfo(),
            (sp, id) => FieldMembershipCategoryController(sp).GetFieldMembershipCategoryById(id),
            sp => FieldMembershipCategoryController(sp).GetAllFieldMembershipCategory(),
            (sp, data) => FieldMembershipCategoryController(sp).PostFieldMembershipCategory(data),
            (sp, id, data) => FieldMembershipCategoryController(sp).PutFieldMembershipCategoryById(id, data),
            (sp, id) => FieldMembershipCategoryController(sp).DeleteFieldMembershipCategoryById(id));
    }

    private static void AddUsageStatisticsTool(IServiceCollection services)
    {
        services.AddLegacyMcpTool("field_usage_statistics.get", "Retrieve usage statistics for the Field microservice.", null,
            (sp, _, ct) => Invoke(ct, () => FieldUsageStatisticsController(sp).GetFieldUsageStatistics()));
    }

    private static void AddCrudTools<TModel>(
        IServiceCollection services,
        string prefix,
        string bodyName,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<Guid>>> getAllIds,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<OSDC.DotnetLibraries.General.DataManagement.MetaInfo?>>> getAllMetaInfo,
        Func<IServiceProvider, Guid, ActionResult<TModel?>> getById,
        Func<IServiceProvider, ActionResult<System.Collections.Generic.IEnumerable<TModel?>>> getAll,
        Func<IServiceProvider, TModel?, ActionResult> create,
        Func<IServiceProvider, Guid, TModel?, ActionResult> update,
        Func<IServiceProvider, Guid, ActionResult> delete)
    {
        services.AddLegacyMcpTool($"{prefix}.get_all_ids", $"Retrieve all {prefix} identifiers.", null,
            (sp, _, ct) => Invoke(ct, () => getAllIds(sp)));
        services.AddLegacyMcpTool($"{prefix}.get_all_meta_info", $"Retrieve metadata for all {prefix} records.", null,
            (sp, _, ct) => Invoke(ct, () => getAllMetaInfo(sp)));
        services.AddLegacyMcpTool($"{prefix}.get_by_id", $"Retrieve a {prefix} record by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
            (sp, args, ct) => InvokeById(args, ct, id => getById(sp, id)));
        services.AddLegacyMcpTool($"{prefix}.get_all", $"Retrieve all {prefix} records with full data.", null,
            (sp, _, ct) => Invoke(ct, () => getAll(sp)));
        services.AddLegacyMcpTool($"{prefix}.create", $"Create a {prefix} record.", McpToolArgumentHelpers.CreateObjectSchema(bodyName),
            (sp, args, ct) => InvokeWithBody<TModel>(args, bodyName, ct, data => create(sp, data)));
        services.AddLegacyMcpTool($"{prefix}.update_by_id", $"Update an existing {prefix} record identified by id.", McpToolArgumentHelpers.CreateObjectSchema(bodyName, includeId: true),
            (sp, args, ct) => InvokeWithIdAndBody<TModel>(args, bodyName, ct, (id, data) => update(sp, id, data)));
        services.AddLegacyMcpTool($"{prefix}.delete_by_id", $"Delete a {prefix} record by identifier.", McpToolArgumentHelpers.CreateGuidSchema("id"),
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

    private static Task<JsonNode?> InvokeWithIdAndBody<TModel>(JsonObject? arguments, string bodyName, CancellationToken cancellationToken, Func<Guid, TModel?, ActionResult> action)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!McpToolArgumentHelpers.TryParseGuid(arguments, "id", out Guid id, out JsonNode? idError))
        {
            return Task.FromResult<JsonNode?>(idError);
        }
        if (!TryDeserialize(arguments, bodyName, out TModel? data, out JsonNode? dataError))
        {
            return Task.FromResult<JsonNode?>(dataError);
        }
        return Task.FromResult<JsonNode?>(McpActionResultConverter.FromActionResult(action(id, data)));
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

    private static FieldCartographicConversionSetController FieldCartographicConversionSetController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldCartographicConversionSetManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldDelineationLineTypeController FieldDelineationLineTypeController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldDelineationLineTypeManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldFeatureCategoryController FieldFeatureCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldFeatureCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldIdentityController FieldIdentityController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldIdentityManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldMembershipCategoryController FieldMembershipCategoryController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldMembershipCategoryManager>>(), sp.GetRequiredService<SqlConnectionManager>());

    private static FieldUsageStatisticsController FieldUsageStatisticsController(IServiceProvider sp) =>
        new(sp.GetRequiredService<ILogger<FieldUsageStatisticsController>>());
}
