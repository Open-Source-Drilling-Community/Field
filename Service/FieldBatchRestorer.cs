using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Model;
using FieldModel = OSDC.Drilling.Field.Model.Field;

namespace OSDC.Drilling.Field.Service;

public enum FieldBatchRestoreFailureKind { None, InvalidRequest, Conflict, StorageFailure }

public sealed class FieldBatchRestoreOutcome
{
    public FieldBatchRestoreResponse? Response { get; init; }
    public FieldBatchErrorEnvelope? Error { get; init; }
    public FieldBatchRestoreFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response != null && FailureKind == FieldBatchRestoreFailureKind.None;
}

/// <summary>Maps portable catalog references and commits catalog and field changes atomically.</summary>
public static class FieldBatchRestorer
{
    public static FieldBatchRestoreOutcome Restore(SqliteConnection connection, FieldBatchRestoreRequest? request, DateTimeOffset restoredAtUtc)
    {
        List<FieldBatchError> validationErrors = Validate(request);
        if (validationErrors.Count != 0)
            return Failure(FieldBatchRestoreFailureKind.InvalidRequest, "invalid_batch_restore_request", "The field batch-restore request is invalid.", validationErrors);

        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            CatalogState catalogs = CatalogState.Load(connection, transaction);
            List<FieldModel> fields = CloneFields(request!.Document!.Fields);
            List<FieldBatchCatalogMapping> mappings = [];
            List<FieldBatchError> mappingErrors = [];
            int createdDefinitions = 0, createdOptions = 0;

            ResolveDependencies(request.Document.CatalogDependencies!, catalogs,
                request.CatalogPolicy == FieldBatchCatalogRestorePolicy.MapOrCreateMissing,
                mappings, mappingErrors, restoredAtUtc, ref createdDefinitions, ref createdOptions);
            if (mappingErrors.Count == 0) RewriteReferences(fields, mappings);

            if (mappingErrors.Count != 0)
            {
                transaction.Rollback();
                return Failure(FieldBatchRestoreFailureKind.Conflict, "catalog_restore_conflict",
                    "No definitions or fields were restored because catalog references could not be resolved unambiguously.", mappingErrors);
            }

            List<PreparedField> prepared = PrepareFields(fields);
            List<bool> exists = prepared.Select(field => RowExists(connection, transaction, "FieldTable", field.ID)).ToList();
            if (request.ConflictPolicy == FieldBatchRestoreConflictPolicy.FailIfExists)
            {
                List<FieldBatchError> conflicts = prepared.Select((field, index) => (field, index))
                    .Where(value => exists[value.index])
                    .Select(value => Error(value.index, "Document.Fields", "field_already_exists", $"A stored field already has UUID '{value.field.ID}'."))
                    .ToList();
                if (conflicts.Count != 0)
                {
                    transaction.Rollback();
                    return Failure(FieldBatchRestoreFailureKind.Conflict, "field_restore_conflict",
                        "No definitions or fields were restored because one or more field UUIDs already exist.", conflicts);
                }
            }

            catalogs.Save(connection, transaction);
            SaveFields(connection, transaction, prepared, request.ConflictPolicy);
            transaction.Commit();
            return new FieldBatchRestoreOutcome
            {
                Response = new FieldBatchRestoreResponse
                {
                    RestoredAtUtc = restoredAtUtc.ToUniversalTime(), CreatedCount = exists.Count(value => !value),
                    ReplacedCount = exists.Count(value => value), CreatedCatalogDefinitionCount = createdDefinitions,
                    CreatedCatalogOptionCount = createdOptions, CatalogMappings = mappings,
                    FieldIDs = prepared.Select(field => field.ID).ToList()
                }
            };
        }
        catch (Exception ex) when (ex is SqliteException or JsonException or NotSupportedException or InvalidOperationException or KeyNotFoundException)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return StorageFailure($"The field database rejected the batch. No changes were committed. {ex.Message}");
        }
    }

    public static FieldBatchRestoreOutcome StorageFailure(string message) => Failure(FieldBatchRestoreFailureKind.StorageFailure,
        "field_restore_failed", message, [Error(null, "Document.Fields", "storage_failure", "The complete restore transaction was rolled back.")]);

    private static void ResolveDependencies(FieldBatchCatalogDependencies dependencies, CatalogState local, bool createMissing,
        List<FieldBatchCatalogMapping> mappings, List<FieldBatchError> errors, DateTimeOffset now,
        ref int createdDefinitions, ref int createdOptions)
    {
        foreach (FieldFeatureCategory source in dependencies.FeatureCategories ?? [])
            ResolveFeatureCategory(source, local, createMissing, mappings, errors, now, ref createdDefinitions, ref createdOptions);
        foreach (FieldMembershipCategory source in dependencies.MembershipCategories ?? [])
            ResolveMembershipCategory(source, local, createMissing, mappings, errors, now, ref createdDefinitions, ref createdOptions);

        foreach (FieldIdentity source in dependencies.Identities ?? [])
        {
            Guid sourceId = source.MetaInfo!.ID;
            FieldIdentity? target = ResolveFlat(sourceId, source.Name, local.Identities, value => value.MetaInfo!.ID,
                value => value.Name, "identity", createMissing, errors);
            bool created = false;
            if (target == null && createMissing && !HasErrorFor(errors, sourceId))
            {
                target = new FieldIdentity { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    CreationDate = now, LastModificationDate = now };
                local.Identities.Add(target); local.DirtyIdentities.Add(target); createdDefinitions++; created = true;
            }
            if (target != null) AddMapping(mappings, "Identity", source.Name, sourceId, target.MetaInfo!.ID,
                sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        }
        foreach (FieldDelineationLineType source in dependencies.DelineationLineTypes ?? [])
        {
            Guid sourceId = source.MetaInfo!.ID;
            FieldDelineationLineType? target = ResolveFlat(sourceId, source.Name, local.LineTypes, value => value.MetaInfo!.ID,
                value => value.Name, "delineation line type", createMissing, errors);
            bool created = false;
            if (target == null && createMissing && !HasErrorFor(errors, sourceId))
            {
                target = new FieldDelineationLineType { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                    CreationDate = now, LastModificationDate = now };
                local.LineTypes.Add(target); local.DirtyLineTypes.Add(target); createdDefinitions++; created = true;
            }
            if (target != null) AddMapping(mappings, "DelineationLineType", source.Name, sourceId, target.MetaInfo!.ID,
                sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        }
    }

    private static void ResolveFeatureCategory(FieldFeatureCategory source, CatalogState local, bool createMissing,
        List<FieldBatchCatalogMapping> mappings, List<FieldBatchError> errors, DateTimeOffset now,
        ref int createdDefinitions, ref int createdOptions)
    {
        Guid sourceId = source.MetaInfo!.ID;
        FieldFeatureCategory? target = ResolveCategory(sourceId, source.Name, source.IsExclusive, source.HasValidityPeriod,
            local.Features, value => value.MetaInfo!.ID, value => value.Name, value => value.IsExclusive,
            value => value.HasValidityPeriod, "feature category", createMissing, errors);
        bool created = false;
        if (target == null && createMissing && !HasErrorFor(errors, sourceId))
        {
            target = new FieldFeatureCategory { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod, Options = [],
                CreationDate = now, LastModificationDate = now };
            local.Features.Add(target); local.DirtyFeatures.Add(target); createdDefinitions++; created = true;
        }
        if (target == null) return;
        AddMapping(mappings, "FeatureCategory", source.Name, sourceId, target.MetaInfo!.ID,
            sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        foreach (FieldFeatureOption option in source.Options ?? [])
        {
            FieldFeatureOption? localOption = ResolveOption(option.ID, option.Name, target.Options ?? [], value => value.ID,
                value => value.Name, "feature option", target.Name, createMissing, errors);
            bool optionCreated = false;
            if (localOption == null && createMissing && !HasErrorFor(errors, option.ID))
            {
                localOption = new FieldFeatureOption { ID = Guid.NewGuid(), Name = option.Name };
                target.Options ??= []; target.Options.Add(localOption); target.LastModificationDate = now;
                local.DirtyFeatures.Add(target); createdOptions++; optionCreated = true;
            }
            if (localOption != null) AddMapping(mappings, "FeatureOption", option.Name, option.ID, localOption.ID,
                option.ID == localOption.ID ? "exact_uuid" : optionCreated ? "created" : "normalized_name");
        }
    }

    private static void ResolveMembershipCategory(FieldMembershipCategory source, CatalogState local, bool createMissing,
        List<FieldBatchCatalogMapping> mappings, List<FieldBatchError> errors, DateTimeOffset now,
        ref int createdDefinitions, ref int createdOptions)
    {
        Guid sourceId = source.MetaInfo!.ID;
        FieldMembershipCategory? target = ResolveCategory(sourceId, source.Name, source.IsExclusive, source.HasValidityPeriod,
            local.Memberships, value => value.MetaInfo!.ID, value => value.Name, value => value.IsExclusive,
            value => value.HasValidityPeriod, "membership category", createMissing, errors);
        bool created = false;
        if (target == null && createMissing && !HasErrorFor(errors, sourceId))
        {
            target = new FieldMembershipCategory { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = source.Name,
                IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod, Options = [],
                CreationDate = now, LastModificationDate = now };
            local.Memberships.Add(target); local.DirtyMemberships.Add(target); createdDefinitions++; created = true;
        }
        if (target == null) return;
        AddMapping(mappings, "MembershipCategory", source.Name, sourceId, target.MetaInfo!.ID,
            sourceId == target.MetaInfo.ID ? "exact_uuid" : created ? "created" : "normalized_name");
        foreach (FieldMembershipOption option in source.Options ?? [])
        {
            FieldMembershipOption? localOption = ResolveOption(option.ID, option.Name, target.Options ?? [], value => value.ID,
                value => value.Name, "membership option", target.Name, createMissing, errors);
            bool optionCreated = false;
            if (localOption == null && createMissing && !HasErrorFor(errors, option.ID))
            {
                localOption = new FieldMembershipOption { ID = Guid.NewGuid(), Name = option.Name };
                target.Options ??= []; target.Options.Add(localOption); target.LastModificationDate = now;
                local.DirtyMemberships.Add(target); createdOptions++; optionCreated = true;
            }
            if (localOption != null) AddMapping(mappings, "MembershipOption", option.Name, option.ID, localOption.ID,
                option.ID == localOption.ID ? "exact_uuid" : optionCreated ? "created" : "normalized_name");
        }
    }

    private static T? ResolveCategory<T>(Guid sourceId, string? sourceName, bool exclusive, bool validity, List<T> local,
        Func<T, Guid> id, Func<T, string?> name, Func<T, bool> isExclusive, Func<T, bool> hasValidity,
        string kind, bool createMissing, List<FieldBatchError> errors)
    {
        T? exact = local.FirstOrDefault(value => id(value) == sourceId);
        if (exact != null)
        {
            if (!SameName(name(exact), sourceName) || isExclusive(exact) != exclusive || hasValidity(exact) != validity)
            { AddSemanticConflict(errors, kind, sourceId, sourceName); return default; }
            return exact;
        }
        List<T> matches = local.Where(value => SameName(name(value), sourceName)).ToList();
        if (matches.Count > 1) { AddAmbiguous(errors, kind, sourceId, sourceName); return default; }
        if (matches.Count == 1)
        {
            T match = matches[0];
            if (isExclusive(match) != exclusive || hasValidity(match) != validity)
            { AddSemanticConflict(errors, kind, sourceId, sourceName); return default; }
            return match;
        }
        if (!createMissing) AddMissing(errors, kind, sourceId, sourceName);
        return default;
    }

    private static T? ResolveFlat<T>(Guid sourceId, string? sourceName, List<T> local, Func<T, Guid> id,
        Func<T, string?> name, string kind, bool createMissing, List<FieldBatchError> errors)
    {
        T? exact = local.FirstOrDefault(value => id(value) == sourceId);
        if (exact != null)
        {
            if (!SameName(name(exact), sourceName)) { AddSemanticConflict(errors, kind, sourceId, sourceName); return default; }
            return exact;
        }
        List<T> matches = local.Where(value => SameName(name(value), sourceName)).ToList();
        if (matches.Count == 1) return matches[0];
        if (matches.Count > 1) AddAmbiguous(errors, kind, sourceId, sourceName);
        else if (!createMissing) AddMissing(errors, kind, sourceId, sourceName);
        return default;
    }

    private static T? ResolveOption<T>(Guid sourceId, string? sourceName, List<T> local, Func<T, Guid> id,
        Func<T, string?> name, string kind, string? categoryName, bool createMissing, List<FieldBatchError> errors)
    {
        T? exact = local.FirstOrDefault(value => id(value) == sourceId);
        if (exact != null)
        {
            if (!SameName(name(exact), sourceName)) AddSemanticConflict(errors, kind, sourceId, sourceName);
            return HasErrorFor(errors, sourceId) ? default : exact;
        }
        List<T> matches = local.Where(value => SameName(name(value), sourceName)).ToList();
        if (matches.Count == 1) return matches[0];
        if (matches.Count > 1) AddAmbiguous(errors, $"{kind} in category '{categoryName}'", sourceId, sourceName);
        else if (!createMissing) AddMissing(errors, $"{kind} in category '{categoryName}'", sourceId, sourceName);
        return default;
    }

    private static void RewriteReferences(List<FieldModel> fields, List<FieldBatchCatalogMapping> mappings)
    {
        Dictionary<Guid, Guid> map = mappings.ToDictionary(value => value.SourceID, value => value.LocalID);
        foreach (FieldModel field in fields)
        {
            foreach (FieldFeatureAssignment assignment in field.FieldFeatureAssignments ?? [])
            { if (assignment.FeatureCategoryID is Guid category) assignment.FeatureCategoryID = map[category]; if (assignment.FeatureOptionID is Guid option) assignment.FeatureOptionID = map[option]; }
            foreach (FieldMembershipAssignment assignment in field.FieldMembershipAssignments ?? [])
            { if (assignment.MembershipCategoryID is Guid category) assignment.MembershipCategoryID = map[category]; if (assignment.MembershipOptionID is Guid option) assignment.MembershipOptionID = map[option]; }
            foreach (FieldIdentityAssignment assignment in field.FieldIdentityAssignments ?? []) if (assignment.IdentityID is Guid id) assignment.IdentityID = map[id];
            foreach (FieldDelineationLine line in field.DelineationLines ?? []) if (line.DelineationLineTypeID is Guid id) line.DelineationLineTypeID = map[id];
        }
    }

    private static List<FieldModel> CloneFields(List<FieldModel> fields) => JsonSerializer.Deserialize<List<FieldModel>>(
        JsonSerializer.Serialize(fields, JsonSettings.Options), JsonSettings.Options) ?? throw new JsonException("Fields could not be cloned.");
    private static List<PreparedField> PrepareFields(List<FieldModel> fields) => fields.Select(field => new PreparedField(
        field.MetaInfo!.ID, JsonSerializer.Serialize(field.MetaInfo, JsonSettings.Options), JsonSerializer.Serialize(field, JsonSettings.Options))).ToList();
    private static bool RowExists(SqliteConnection connection, SqliteTransaction transaction, string table, Guid id)
    { using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE ID=$id"; command.Parameters.AddWithValue("$id", id.ToString()); return Convert.ToInt64(command.ExecuteScalar()) != 0; }
    private static void SaveFields(SqliteConnection connection, SqliteTransaction transaction, List<PreparedField> fields, FieldBatchRestoreConflictPolicy policy)
    {
        foreach (PreparedField field in fields)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = policy == FieldBatchRestoreConflictPolicy.ReplaceExisting
                ? "INSERT INTO FieldTable (ID,MetaInfo,Field) VALUES ($id,$meta,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,Field=excluded.Field"
                : "INSERT INTO FieldTable (ID,MetaInfo,Field) VALUES ($id,$meta,$doc)";
            command.Parameters.AddWithValue("$id", field.ID.ToString()); command.Parameters.AddWithValue("$meta", field.MetaInfoJson); command.Parameters.AddWithValue("$doc", field.FieldJson); command.ExecuteNonQuery();
        }
    }

    private static List<FieldBatchError> Validate(FieldBatchRestoreRequest? request)
    {
        if (request == null) return [Error(null, "Request", "required", "A batch-restore request is required.")];
        List<FieldBatchError> errors = [];
        if (request.ConflictPolicy is not FieldBatchRestoreConflictPolicy.FailIfExists and not FieldBatchRestoreConflictPolicy.ReplaceExisting)
            errors.Add(Error(null, "ConflictPolicy", "invalid_conflict_policy", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        FieldBatchExportDocument? document = request.Document;
        if (document == null) { errors.Add(Error(null, "Document", "required", "A batch-export document is required.")); return errors; }
        if (document.FormatIdentifier != FieldBatchExportDocument.CurrentFormatIdentifier) errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format", $"FormatIdentifier must be '{FieldBatchExportDocument.CurrentFormatIdentifier}'."));
        if (document.SchemaVersion != FieldBatchExportDocument.CurrentSchemaVersion) errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version", $"SchemaVersion must be {FieldBatchExportDocument.CurrentSchemaVersion}."));
        if (document.SchemaVersion == FieldBatchExportDocument.CurrentSchemaVersion)
        {
            if (document.CatalogDependencies == null) errors.Add(Error(null, "Document.CatalogDependencies", "required", "Schema version 2 requires catalog dependencies."));
            if (request.CatalogPolicy is not FieldBatchCatalogRestorePolicy.MapExisting and not FieldBatchCatalogRestorePolicy.MapOrCreateMissing) errors.Add(Error(null, "CatalogPolicy", "invalid_catalog_policy", "CatalogPolicy must be MapExisting or MapOrCreateMissing for schema version 2."));
            if (document.CatalogDependencies != null)
            {
                ValidateDependencies(document.CatalogDependencies, errors);
                ValidateDependencyReferences(document.Fields ?? [], document.CatalogDependencies, errors);
            }
        }
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero) errors.Add(Error(null, "Document.ExportedAtUtc", "invalid_export_timestamp", "ExportedAtUtc must be a non-default UTC timestamp with offset +00:00."));
        if (document.Fields == null || document.Fields.Count == 0) { errors.Add(Error(null, "Document.Fields", "required", "At least one field is required for restore.")); return errors; }
        Dictionary<Guid, int> positions = [];
        for (int index = 0; index < document.Fields.Count; index++)
        {
            FieldModel? field = document.Fields[index]; Guid? id = field?.MetaInfo?.ID;
            if (field == null) { errors.Add(Error(index, "Document.Fields", "null_field", "A restored field must not be null.")); continue; }
            if (id == null || id == Guid.Empty) errors.Add(Error(index, "Document.Fields.MetaInfo.ID", "empty_uuid", "Every restored field must have a non-empty UUID."));
            else if (positions.TryGetValue(id.Value, out int first)) errors.Add(Error(index, "Document.Fields.MetaInfo.ID", "duplicate_uuid", $"Field UUID '{id}' duplicates position {first}.")); else positions.Add(id.Value, index);
            if (field.ProjectionDefinitionID == Guid.Empty) errors.Add(Error(index, "Document.Fields.ProjectionDefinitionID", "empty_uuid", "ProjectionDefinitionID must be omitted or a non-empty UUID."));
        }
        return errors;
    }

    private static void ValidateDependencies(FieldBatchCatalogDependencies value, List<FieldBatchError> errors)
    {
        HashSet<Guid> ids = [];
        void Check(Guid id, string? name, string property)
        { if (id == Guid.Empty) errors.Add(Error(null, property, "empty_uuid", "Catalog source UUIDs must be non-empty.")); else if (!ids.Add(id)) errors.Add(Error(null, property, "duplicate_uuid", $"Catalog source UUID '{id}' occurs more than once.")); if (string.IsNullOrWhiteSpace(name)) errors.Add(Error(null, property + ".Name", "required", "Catalog names must not be empty.")); }
        foreach (FieldFeatureCategory? category in value.FeatureCategories ?? []) { Check(category?.MetaInfo?.ID ?? Guid.Empty, category?.Name, "Document.CatalogDependencies.FeatureCategories"); foreach (FieldFeatureOption option in category?.Options ?? []) Check(option.ID, option.Name, "Document.CatalogDependencies.FeatureCategories.Options"); }
        foreach (FieldMembershipCategory? category in value.MembershipCategories ?? []) { Check(category?.MetaInfo?.ID ?? Guid.Empty, category?.Name, "Document.CatalogDependencies.MembershipCategories"); foreach (FieldMembershipOption option in category?.Options ?? []) Check(option.ID, option.Name, "Document.CatalogDependencies.MembershipCategories.Options"); }
        foreach (FieldIdentity? identity in value.Identities ?? []) Check(identity?.MetaInfo?.ID ?? Guid.Empty, identity?.Name, "Document.CatalogDependencies.Identities");
        foreach (FieldDelineationLineType? lineType in value.DelineationLineTypes ?? []) Check(lineType?.MetaInfo?.ID ?? Guid.Empty, lineType?.Name, "Document.CatalogDependencies.DelineationLineTypes");
    }

    private static void ValidateDependencyReferences(List<FieldModel> fields, FieldBatchCatalogDependencies dependencies,
        List<FieldBatchError> errors)
    {
        Dictionary<Guid, HashSet<Guid>> featureOptions = (dependencies.FeatureCategories ?? [])
            .Where(value => value?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .ToDictionary(value => value.MetaInfo!.ID, value => (value.Options ?? []).Select(option => option.ID).ToHashSet());
        Dictionary<Guid, HashSet<Guid>> membershipOptions = (dependencies.MembershipCategories ?? [])
            .Where(value => value?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .ToDictionary(value => value.MetaInfo!.ID, value => (value.Options ?? []).Select(option => option.ID).ToHashSet());
        HashSet<Guid> identityIds = (dependencies.Identities ?? []).Select(value => value?.MetaInfo?.ID ?? Guid.Empty).ToHashSet();
        HashSet<Guid> lineTypeIds = (dependencies.DelineationLineTypes ?? []).Select(value => value?.MetaInfo?.ID ?? Guid.Empty).ToHashSet();
        for (int index = 0; index < fields.Count; index++)
        {
            foreach (FieldFeatureAssignment assignment in fields[index]?.FieldFeatureAssignments ?? [])
                RequireHierarchicalDependency(assignment.FeatureCategoryID, assignment.FeatureOptionID, featureOptions,
                    index, "FieldFeatureAssignments", errors);
            foreach (FieldMembershipAssignment assignment in fields[index]?.FieldMembershipAssignments ?? [])
                RequireHierarchicalDependency(assignment.MembershipCategoryID, assignment.MembershipOptionID, membershipOptions,
                    index, "FieldMembershipAssignments", errors);
            foreach (FieldIdentityAssignment assignment in fields[index]?.FieldIdentityAssignments ?? []) RequireDependency(assignment.IdentityID, identityIds, index, "FieldIdentityAssignments.IdentityID", errors);
            foreach (FieldDelineationLine line in fields[index]?.DelineationLines ?? []) RequireDependency(line.DelineationLineTypeID, lineTypeIds, index, "DelineationLines.DelineationLineTypeID", errors);
        }
    }

    private static void RequireHierarchicalDependency(Guid? categoryId, Guid? optionId,
        Dictionary<Guid, HashSet<Guid>> optionsByCategory, int index, string property, List<FieldBatchError> errors)
    {
        if (categoryId is not Guid category || category == Guid.Empty || !optionsByCategory.TryGetValue(category, out HashSet<Guid>? options))
        {
            errors.Add(Error(index, $"Document.Fields.{property}.CategoryID", "catalog_dependency_missing",
                $"Referenced category UUID '{categoryId}' is absent from the corresponding CatalogDependencies collection."));
            return;
        }
        if (optionId is not Guid option || option == Guid.Empty || !options.Contains(option))
            errors.Add(Error(index, $"Document.Fields.{property}.OptionID", "catalog_dependency_missing",
                $"Referenced option UUID '{optionId}' is absent from category '{category}'."));
    }

    private static void RequireDependency(Guid? id, HashSet<Guid> available, int index, string property, List<FieldBatchError> errors)
    {
        if (id is not Guid value || value == Guid.Empty)
            errors.Add(Error(index, $"Document.Fields.{property}", "invalid_catalog_reference", "Catalog references must be non-empty UUIDs."));
        else if (!available.Contains(value))
            errors.Add(Error(index, $"Document.Fields.{property}", "catalog_dependency_missing", $"Referenced UUID '{value}' is absent from CatalogDependencies."));
    }

    private static string Normalize(string? value) => string.Join(' ', (value ?? "").Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static bool SameName(string? left, string? right) => Normalize(left) == Normalize(right);
    private static bool HasErrorFor(List<FieldBatchError> errors, Guid id) => errors.Any(error => error.Message.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
    private static void AddMissing(List<FieldBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_definition_missing", $"No compatible local {kind} exists for '{name}' ({id}), and creation is disabled."));
    private static void AddAmbiguous(List<FieldBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "ambiguous_catalog_match", $"More than one local {kind} has normalized name '{name}' for source UUID '{id}'."));
    private static void AddSemanticConflict(List<FieldBatchError> errors, string kind, Guid id, string? name) => errors.Add(Error(null, $"Document.CatalogDependencies[{id}]", "catalog_semantic_conflict", $"The local {kind} corresponding to '{name}' ({id}) has incompatible semantics."));
    private static void AddMapping(List<FieldBatchCatalogMapping> mappings, string catalog, string? name, Guid source, Guid local, string resolution) => mappings.Add(new() { Catalog = catalog, Name = name ?? "", SourceID = source, LocalID = local, Resolution = resolution });
    private static FieldBatchRestoreOutcome Failure(FieldBatchRestoreFailureKind kind, string error, string message, List<FieldBatchError> errors) => new() { FailureKind = kind, Error = new() { Error = error, Message = message, Errors = errors } };
    private static FieldBatchError Error(int? index, string property, string code, string message) => new() { PositionIndex = index, Property = property, Code = code, Message = message };
    private sealed record PreparedField(Guid ID, string MetaInfoJson, string FieldJson);

    private sealed class CatalogState
    {
        public List<FieldFeatureCategory> Features { get; } = [];
        public List<FieldMembershipCategory> Memberships { get; } = [];
        public List<FieldIdentity> Identities { get; } = [];
        public List<FieldDelineationLineType> LineTypes { get; } = [];
        public HashSet<FieldFeatureCategory> DirtyFeatures { get; } = [];
        public HashSet<FieldMembershipCategory> DirtyMemberships { get; } = [];
        public HashSet<FieldIdentity> DirtyIdentities { get; } = [];
        public HashSet<FieldDelineationLineType> DirtyLineTypes { get; } = [];

        public static CatalogState Load(SqliteConnection connection, SqliteTransaction transaction)
        {
            CatalogState state = new();
            state.Features.AddRange(Read<FieldFeatureCategory>(connection, transaction, "FieldFeatureCategoryTable", "FieldFeatureCategory"));
            state.Memberships.AddRange(Read<FieldMembershipCategory>(connection, transaction, "FieldMembershipCategoryTable", "FieldMembershipCategory"));
            state.Identities.AddRange(Read<FieldIdentity>(connection, transaction, "FieldIdentityTable", "FieldIdentity"));
            state.LineTypes.AddRange(Read<FieldDelineationLineType>(connection, transaction, "FieldDelineationLineTypeTable", "FieldDelineationLineType"));
            return state;
        }
        private static List<T> Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
        { using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = $"SELECT {column} FROM {table}"; using SqliteDataReader reader = command.ExecuteReader(); List<T> result = []; while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options) ?? throw new JsonException($"Invalid {table} document.")); return result; }
        public void Save(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (FieldFeatureCategory value in DirtyFeatures) UpsertCategory(connection, transaction, "FieldFeatureCategoryTable", "FieldFeatureCategory", value.MetaInfo!, value.Name, value.IsExclusive, value.HasValidityPeriod, value.CreationDate, value.LastModificationDate, value);
            foreach (FieldMembershipCategory value in DirtyMemberships) UpsertCategory(connection, transaction, "FieldMembershipCategoryTable", "FieldMembershipCategory", value.MetaInfo!, value.Name, value.IsExclusive, value.HasValidityPeriod, value.CreationDate, value.LastModificationDate, value);
            foreach (FieldIdentity value in DirtyIdentities) InsertFlat(connection, transaction, "FieldIdentityTable", "FieldIdentity", value.MetaInfo!, value.Name, value.CreationDate, value.LastModificationDate, value);
            foreach (FieldDelineationLineType value in DirtyLineTypes) InsertFlat(connection, transaction, "FieldDelineationLineTypeTable", "FieldDelineationLineType", value.MetaInfo!, value.Name, value.CreationDate, value.LastModificationDate, value);
        }
        private static void UpsertCategory(SqliteConnection c, SqliteTransaction t, string table, string column, MetaInfo meta, string? name, bool exclusive, bool validity, DateTimeOffset? created, DateTimeOffset? modified, object document)
        { using SqliteCommand cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = $"INSERT INTO {table} (ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,CreationDate,LastModificationDate,{column}) VALUES ($id,$meta,$name,$exclusive,$validity,$created,$modified,$doc) ON CONFLICT(ID) DO UPDATE SET MetaInfo=excluded.MetaInfo,Name=excluded.Name,IsExclusive=excluded.IsExclusive,HasValidityPeriod=excluded.HasValidityPeriod,CreationDate=excluded.CreationDate,LastModificationDate=excluded.LastModificationDate,{column}=excluded.{column}"; AddCommon(cmd, meta, name, created, modified, document); cmd.Parameters.AddWithValue("$exclusive", exclusive ? 1 : 0); cmd.Parameters.AddWithValue("$validity", validity ? 1 : 0); cmd.ExecuteNonQuery(); }
        private static void InsertFlat(SqliteConnection c, SqliteTransaction t, string table, string column, MetaInfo meta, string? name, DateTimeOffset? created, DateTimeOffset? modified, object document)
        { using SqliteCommand cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = $"INSERT INTO {table} (ID,MetaInfo,Name,CreationDate,LastModificationDate,{column}) VALUES ($id,$meta,$name,$created,$modified,$doc)"; AddCommon(cmd, meta, name, created, modified, document); cmd.ExecuteNonQuery(); }
        private static void AddCommon(SqliteCommand cmd, MetaInfo meta, string? name, DateTimeOffset? created, DateTimeOffset? modified, object document)
        { cmd.Parameters.AddWithValue("$id", meta.ID.ToString()); cmd.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta, JsonSettings.Options)); cmd.Parameters.AddWithValue("$name", name ?? ""); cmd.Parameters.AddWithValue("$created", created?.ToString(Managers.SqlConnectionManager.DATE_TIME_FORMAT) ?? ""); cmd.Parameters.AddWithValue("$modified", modified?.ToString(Managers.SqlConnectionManager.DATE_TIME_FORMAT) ?? ""); cmd.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(document, JsonSettings.Options)); }
    }
}
