using System;
using System.Collections.Generic;
using System.Linq;
using OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.Service;

public enum FieldBatchExportFailureKind
{
    None = 0,
    InvalidRequest = 1,
    FieldNotFound = 2,
    StorageFailure = 3
}

public sealed class FieldBatchExportOutcome
{
    public FieldBatchExportDocument? Document { get; init; }
    public FieldBatchErrorEnvelope? Error { get; init; }
    public FieldBatchExportFailureKind FailureKind { get; init; }

    public bool IsSuccess => Document != null && FailureKind == FieldBatchExportFailureKind.None;
}

/// <summary>
/// Validates a batch-export request and creates an immutable response document
/// from one database snapshot supplied by the caller.
/// </summary>
public static class FieldBatchExporter
{
    public static FieldBatchExportOutcome StorageFailure(string message)
    {
        return Failure(FieldBatchExportFailureKind.StorageFailure, "field_export_failed", message,
            [Error(null, "Document", "storage_failure", "The export snapshot could not be produced.")]);
    }

    public static FieldBatchExportOutcome Create(
        FieldBatchExportRequest? request,
        IEnumerable<Model.Field?> snapshot,
        DateTimeOffset exportedAtUtc,
        IEnumerable<FieldFeatureCategory>? featureCategories = null,
        IEnumerable<FieldMembershipCategory>? membershipCategories = null,
        IEnumerable<FieldIdentity>? identities = null,
        IEnumerable<FieldDelineationLineType>? delineationLineTypes = null)
    {
        List<FieldBatchError> validationErrors = ValidateRequest(request);
        if (validationErrors.Count != 0)
        {
            return Failure(
                FieldBatchExportFailureKind.InvalidRequest,
                "invalid_batch_export_request",
                "The field batch-export request is invalid.",
                validationErrors);
        }

        var fieldsById = new Dictionary<Guid, Model.Field>();
        int storagePosition = 0;
        foreach (Model.Field? field in snapshot)
        {
            Guid? id = field?.MetaInfo?.ID;
            if (field == null || id == null || id == Guid.Empty)
            {
                return Failure(
                    FieldBatchExportFailureKind.StorageFailure,
                    "field_export_failed",
                    "A stored field could not be represented in the export.",
                    [Error(storagePosition, "Fields", "invalid_stored_field", "A stored field is null or has no non-empty UUID.")]);
            }

            if (!fieldsById.TryAdd(id.Value, field))
            {
                return Failure(
                    FieldBatchExportFailureKind.StorageFailure,
                    "field_export_failed",
                    "A stored field could not be represented in the export.",
                    [Error(storagePosition, "Fields", "duplicate_stored_field_id", $"More than one stored field has UUID '{id}'.")]);
            }
            storagePosition++;
        }

        List<Model.Field> exportedFields;
        if (request!.Scope == FieldBatchExportScope.All)
        {
            exportedFields = fieldsById
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .ToList();
        }
        else
        {
            exportedFields = [];
            List<FieldBatchError> missingErrors = [];
            for (int index = 0; index < request.FieldIDs!.Count; index++)
            {
                Guid id = request.FieldIDs[index];
                if (fieldsById.TryGetValue(id, out Model.Field? field))
                {
                    exportedFields.Add(field);
                }
                else
                {
                    missingErrors.Add(Error(index, "FieldIDs", "field_not_found", $"No stored field has UUID '{id}'."));
                }
            }

            if (missingErrors.Count != 0)
            {
                return Failure(
                    FieldBatchExportFailureKind.FieldNotFound,
                    "field_not_found",
                    "The complete selected batch could not be exported because one or more fields do not exist.",
                    missingErrors);
            }
        }

        FieldBatchCatalogDependencies? dependencies = BuildDependencies(
            exportedFields,
            featureCategories ?? [],
            membershipCategories ?? [],
            identities ?? [],
            delineationLineTypes ?? [],
            out List<FieldBatchError> dependencyErrors);
        if (dependencies == null)
        {
            return Failure(
                FieldBatchExportFailureKind.StorageFailure,
                "field_export_dependency_missing",
                "The export could not include every catalog definition referenced by the selected fields.",
                dependencyErrors);
        }

        return new FieldBatchExportOutcome
        {
            Document = new FieldBatchExportDocument
            {
                ExportedAtUtc = exportedAtUtc.ToUniversalTime(),
                CatalogDependencies = dependencies,
                Fields = exportedFields
            }
        };
    }

    private static FieldBatchCatalogDependencies? BuildDependencies(
        IReadOnlyList<Model.Field> fields,
        IEnumerable<FieldFeatureCategory> featureCategories,
        IEnumerable<FieldMembershipCategory> membershipCategories,
        IEnumerable<FieldIdentity> identities,
        IEnumerable<FieldDelineationLineType> delineationLineTypes,
        out List<FieldBatchError> errors)
    {
        errors = [];
        Dictionary<Guid, FieldFeatureCategory> features = Index(featureCategories);
        Dictionary<Guid, FieldMembershipCategory> memberships = Index(membershipCategories);
        Dictionary<Guid, FieldIdentity> identityIndex = Index(identities);
        Dictionary<Guid, FieldDelineationLineType> lineTypes = Index(delineationLineTypes);
        var result = new FieldBatchCatalogDependencies();

        var featureOptionsByCategory = new Dictionary<Guid, HashSet<Guid>>();
        var membershipOptionsByCategory = new Dictionary<Guid, HashSet<Guid>>();
        var identityIds = new HashSet<Guid>();
        var lineTypeIds = new HashSet<Guid>();

        for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            Model.Field field = fields[fieldIndex];
            foreach (FieldFeatureAssignment assignment in field.FieldFeatureAssignments ?? [])
            {
                AddHierarchicalReference(assignment.FeatureCategoryID, assignment.FeatureOptionID,
                    featureOptionsByCategory, fieldIndex, "FieldFeatureAssignments", errors);
            }
            foreach (FieldMembershipAssignment assignment in field.FieldMembershipAssignments ?? [])
            {
                AddHierarchicalReference(assignment.MembershipCategoryID, assignment.MembershipOptionID,
                    membershipOptionsByCategory, fieldIndex, "FieldMembershipAssignments", errors);
            }
            foreach (FieldIdentityAssignment assignment in field.FieldIdentityAssignments ?? [])
            {
                if (assignment.IdentityID is Guid id && id != Guid.Empty)
                {
                    identityIds.Add(id);
                }
            }
            foreach (FieldDelineationLine line in field.DelineationLines ?? [])
            {
                if (line.DelineationLineTypeID is Guid id && id != Guid.Empty)
                {
                    lineTypeIds.Add(id);
                }
            }
        }

        foreach ((Guid categoryId, HashSet<Guid> optionIds) in featureOptionsByCategory.OrderBy(pair => pair.Key))
        {
            if (!features.TryGetValue(categoryId, out FieldFeatureCategory? category))
            {
                errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "referenced_definition_missing",
                    $"Referenced feature category '{categoryId}' does not exist."));
                continue;
            }
            List<FieldFeatureOption> options = SelectOptions(category.Options, optionIds, "feature", categoryId, errors);
            result.FeatureCategories.Add(new FieldFeatureCategory
            {
                MetaInfo = category.MetaInfo,
                Name = category.Name,
                IsExclusive = category.IsExclusive,
                HasValidityPeriod = category.HasValidityPeriod,
                Options = options,
                CreationDate = category.CreationDate,
                LastModificationDate = category.LastModificationDate
            });
        }

        foreach ((Guid categoryId, HashSet<Guid> optionIds) in membershipOptionsByCategory.OrderBy(pair => pair.Key))
        {
            if (!memberships.TryGetValue(categoryId, out FieldMembershipCategory? category))
            {
                errors.Add(Error(null, "CatalogDependencies.MembershipCategories", "referenced_definition_missing",
                    $"Referenced membership category '{categoryId}' does not exist."));
                continue;
            }
            List<FieldMembershipOption> options = SelectOptions(category.Options, optionIds, "membership", categoryId, errors);
            result.MembershipCategories.Add(new FieldMembershipCategory
            {
                MetaInfo = category.MetaInfo,
                Name = category.Name,
                IsExclusive = category.IsExclusive,
                HasValidityPeriod = category.HasValidityPeriod,
                Options = options,
                CreationDate = category.CreationDate,
                LastModificationDate = category.LastModificationDate
            });
        }

        AddFlatDependencies(identityIds, identityIndex, result.Identities, "identity", errors);
        AddFlatDependencies(lineTypeIds, lineTypes, result.DelineationLineTypes, "delineation line type", errors);
        return errors.Count == 0 ? result : null;
    }

    private static Dictionary<Guid, T> Index<T>(IEnumerable<T> definitions) where T : class
    {
        var result = new Dictionary<Guid, T>();
        foreach (T definition in definitions)
        {
            Guid? id = definition switch
            {
                FieldFeatureCategory value => value.MetaInfo?.ID,
                FieldMembershipCategory value => value.MetaInfo?.ID,
                FieldIdentity value => value.MetaInfo?.ID,
                FieldDelineationLineType value => value.MetaInfo?.ID,
                _ => null
            };
            if (id is Guid valueId && valueId != Guid.Empty)
            {
                result.TryAdd(valueId, definition);
            }
        }
        return result;
    }

    private static void AddHierarchicalReference(Guid? categoryId, Guid? optionId,
        Dictionary<Guid, HashSet<Guid>> references, int fieldIndex, string property,
        List<FieldBatchError> errors)
    {
        if (categoryId is not Guid category || category == Guid.Empty || optionId is not Guid option || option == Guid.Empty)
        {
            errors.Add(Error(fieldIndex, $"Document.Fields.{property}", "invalid_catalog_reference",
                "A catalog assignment must reference non-empty category and option UUIDs."));
            return;
        }
        if (!references.TryGetValue(category, out HashSet<Guid>? options))
        {
            options = [];
            references.Add(category, options);
        }
        options.Add(option);
    }

    private static List<FieldFeatureOption> SelectOptions(List<FieldFeatureOption>? available,
        HashSet<Guid> required, string kind, Guid categoryId, List<FieldBatchError> errors)
    {
        Dictionary<Guid, FieldFeatureOption> index = (available ?? []).Where(value => value.ID != Guid.Empty)
            .GroupBy(value => value.ID).ToDictionary(group => group.Key, group => group.First());
        var result = new List<FieldFeatureOption>();
        foreach (Guid id in required.Order())
        {
            if (index.TryGetValue(id, out FieldFeatureOption? option)) result.Add(option);
            else errors.Add(Error(null, "CatalogDependencies.FeatureCategories.Options", "referenced_option_missing",
                $"Referenced {kind} option '{id}' does not exist in category '{categoryId}'."));
        }
        return result;
    }

    private static List<FieldMembershipOption> SelectOptions(List<FieldMembershipOption>? available,
        HashSet<Guid> required, string kind, Guid categoryId, List<FieldBatchError> errors)
    {
        Dictionary<Guid, FieldMembershipOption> index = (available ?? []).Where(value => value.ID != Guid.Empty)
            .GroupBy(value => value.ID).ToDictionary(group => group.Key, group => group.First());
        var result = new List<FieldMembershipOption>();
        foreach (Guid id in required.Order())
        {
            if (index.TryGetValue(id, out FieldMembershipOption? option)) result.Add(option);
            else errors.Add(Error(null, "CatalogDependencies.MembershipCategories.Options", "referenced_option_missing",
                $"Referenced {kind} option '{id}' does not exist in category '{categoryId}'."));
        }
        return result;
    }

    private static void AddFlatDependencies<T>(HashSet<Guid> required, Dictionary<Guid, T> available,
        List<T> target, string kind, List<FieldBatchError> errors) where T : class
    {
        foreach (Guid id in required.Order())
        {
            if (available.TryGetValue(id, out T? definition)) target.Add(definition);
            else errors.Add(Error(null, "CatalogDependencies", "referenced_definition_missing",
                $"Referenced {kind} '{id}' does not exist."));
        }
    }

    private static List<FieldBatchError> ValidateRequest(FieldBatchExportRequest? request)
    {
        if (request == null)
        {
            return [Error(null, "Request", "required", "A batch-export request is required.")];
        }

        List<FieldBatchError> errors = [];
        if (request.Scope is not FieldBatchExportScope.All and not FieldBatchExportScope.Selected)
        {
            errors.Add(Error(null, "Scope", "invalid_scope", "Scope must be All or Selected."));
            return errors;
        }

        if (request.Scope == FieldBatchExportScope.All)
        {
            if (request.FieldIDs is { Count: > 0 })
            {
                errors.Add(Error(null, "FieldIDs", "not_allowed", "FieldIDs must be omitted or empty when Scope is All."));
            }
            return errors;
        }

        if (request.FieldIDs == null || request.FieldIDs.Count == 0)
        {
            errors.Add(Error(null, "FieldIDs", "required", "At least one field UUID is required when Scope is Selected."));
            return errors;
        }

        var positionsById = new Dictionary<Guid, int>();
        for (int index = 0; index < request.FieldIDs.Count; index++)
        {
            Guid id = request.FieldIDs[index];
            if (id == Guid.Empty)
            {
                errors.Add(Error(index, "FieldIDs", "empty_uuid", "Field UUIDs must not be empty."));
            }
            else if (positionsById.TryGetValue(id, out int firstIndex))
            {
                errors.Add(Error(index, "FieldIDs", "duplicate_uuid", $"Field UUID '{id}' duplicates position {firstIndex}."));
            }
            else
            {
                positionsById.Add(id, index);
            }
        }
        return errors;
    }

    private static FieldBatchExportOutcome Failure(
        FieldBatchExportFailureKind kind,
        string error,
        string message,
        List<FieldBatchError> errors)
    {
        return new FieldBatchExportOutcome
        {
            FailureKind = kind,
            Error = new FieldBatchErrorEnvelope
            {
                Error = error,
                Message = message,
                Errors = errors
            }
        };
    }

    private static FieldBatchError Error(int? positionIndex, string property, string code, string message)
    {
        return new FieldBatchError
        {
            PositionIndex = positionIndex,
            Property = property,
            Code = code,
            Message = message
        };
    }
}
