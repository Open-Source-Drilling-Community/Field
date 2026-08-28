using Microsoft.Data.Sqlite;
using OSDC.Drilling.Field.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.Field.Service.Managers;

internal static class FieldReferenceIntegrityValidator
{
    public static List<FieldMutationError> ValidateField(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Model.Field field)
    {
        Dictionary<Guid, HashSet<Guid>> featureOptions = ReadCategoryOptions<FieldFeatureCategory, FieldFeatureOption>(
            connection, transaction, "FieldFeatureCategoryTable", "FieldFeatureCategory",
            category => category.MetaInfo?.ID, category => category.Options, option => option.ID);
        Dictionary<Guid, HashSet<Guid>> membershipOptions = ReadCategoryOptions<FieldMembershipCategory, FieldMembershipOption>(
            connection, transaction, "FieldMembershipCategoryTable", "FieldMembershipCategory",
            category => category.MetaInfo?.ID, category => category.Options, option => option.ID);
        HashSet<Guid> identities = ReadDefinitionIds<FieldIdentity>(connection, transaction, "FieldIdentityTable", "FieldIdentity", value => value.MetaInfo?.ID);
        HashSet<Guid> lineTypes = ReadDefinitionIds<FieldDelineationLineType>(connection, transaction, "FieldDelineationLineTypeTable", "FieldDelineationLineType", value => value.MetaInfo?.ID);

        List<FieldMutationError> errors = [];
        for (int index = 0; index < (field.FieldFeatureAssignments?.Count ?? 0); index++)
        {
            FieldFeatureAssignment assignment = field.FieldFeatureAssignments![index];
            ValidateCategoryReference(assignment.FeatureCategoryID, assignment.FeatureOptionID, featureOptions,
                $"FieldFeatureAssignments[{index}]", "FeatureCategoryID", "FeatureOptionID", errors);
        }
        for (int index = 0; index < (field.FieldMembershipAssignments?.Count ?? 0); index++)
        {
            FieldMembershipAssignment assignment = field.FieldMembershipAssignments![index];
            ValidateCategoryReference(assignment.MembershipCategoryID, assignment.MembershipOptionID, membershipOptions,
                $"FieldMembershipAssignments[{index}]", "MembershipCategoryID", "MembershipOptionID", errors);
        }
        for (int index = 0; index < (field.FieldIdentityAssignments?.Count ?? 0); index++)
        {
            Guid? id = field.FieldIdentityAssignments![index].IdentityID;
            ValidateOptionalReference(id, identities, $"FieldIdentityAssignments[{index}].IdentityID", "field_identity_not_found", errors);
        }
        for (int index = 0; index < (field.DelineationLines?.Count ?? 0); index++)
        {
            Guid? id = field.DelineationLines![index].DelineationLineTypeID;
            ValidateOptionalReference(id, lineTypes, $"DelineationLines[{index}].DelineationLineTypeID", "delineation_line_type_not_found", errors);
        }
        return errors;
    }

    public static FieldMutationError? FindFeatureCategoryReferences(SqliteConnection connection, SqliteTransaction transaction,
        Guid categoryId, IReadOnlyCollection<Guid>? permittedOptionIds = null) =>
        FindReferences(connection, transaction,
            field => (field.FieldFeatureAssignments ?? [])
                .Where(value => value.FeatureCategoryID == categoryId &&
                    (permittedOptionIds == null || value.FeatureOptionID is Guid optionId && !permittedOptionIds.Contains(optionId)))
                .Any(),
            permittedOptionIds == null ? "FieldFeatureAssignments.FeatureCategoryID" : "FieldFeatureAssignments.FeatureOptionID",
            permittedOptionIds == null ? "catalog_in_use" : "catalog_option_in_use",
            permittedOptionIds == null
                ? "The feature category is referenced by one or more Fields."
                : "The update removes a feature option referenced by one or more Fields.");

    public static FieldMutationError? FindMembershipCategoryReferences(SqliteConnection connection, SqliteTransaction transaction,
        Guid categoryId, IReadOnlyCollection<Guid>? permittedOptionIds = null) =>
        FindReferences(connection, transaction,
            field => (field.FieldMembershipAssignments ?? [])
                .Where(value => value.MembershipCategoryID == categoryId &&
                    (permittedOptionIds == null || value.MembershipOptionID is Guid optionId && !permittedOptionIds.Contains(optionId)))
                .Any(),
            permittedOptionIds == null ? "FieldMembershipAssignments.MembershipCategoryID" : "FieldMembershipAssignments.MembershipOptionID",
            permittedOptionIds == null ? "catalog_in_use" : "catalog_option_in_use",
            permittedOptionIds == null
                ? "The membership category is referenced by one or more Fields."
                : "The update removes a membership option referenced by one or more Fields.");

    public static FieldMutationError? FindIdentityReferences(SqliteConnection connection, SqliteTransaction transaction, Guid identityId) =>
        FindReferences(connection, transaction,
            field => (field.FieldIdentityAssignments ?? []).Any(value => value.IdentityID == identityId),
            "FieldIdentityAssignments.IdentityID", "catalog_in_use",
            "The Field identity is referenced by one or more Fields.");

    public static FieldMutationError? FindDelineationLineTypeReferences(SqliteConnection connection, SqliteTransaction transaction, Guid typeId) =>
        FindReferences(connection, transaction,
            field => (field.DelineationLines ?? []).Any(value => value.DelineationLineTypeID == typeId),
            "DelineationLines.DelineationLineTypeID", "catalog_in_use",
            "The delineation line type is referenced by one or more Fields.");

    private static void ValidateCategoryReference(Guid? categoryId, Guid? optionId,
        IReadOnlyDictionary<Guid, HashSet<Guid>> optionsByCategory, string path, string categoryProperty,
        string optionProperty, List<FieldMutationError> errors)
    {
        if (categoryId == null && optionId == null)
        {
            return;
        }
        if (categoryId is not Guid category || category == Guid.Empty)
        {
            errors.Add(Error($"{path}.{categoryProperty}", "category_id_required", "A category UUID is required when an option is selected."));
            return;
        }
        if (!optionsByCategory.TryGetValue(category, out HashSet<Guid>? options))
        {
            errors.Add(Error($"{path}.{categoryProperty}", "category_not_found", $"No local category has UUID {category}."));
            return;
        }
        if (optionId is not Guid option || option == Guid.Empty)
        {
            errors.Add(Error($"{path}.{optionProperty}", "option_id_required", "An option UUID is required when a category is selected."));
            return;
        }
        if (!options.Contains(option))
        {
            errors.Add(Error($"{path}.{optionProperty}", "option_not_in_category", $"Option UUID {option} does not belong to category UUID {category}."));
        }
    }

    private static void ValidateOptionalReference(Guid? id, IReadOnlySet<Guid> knownIds, string property,
        string code, List<FieldMutationError> errors)
    {
        if (id == null)
        {
            return;
        }
        if (id == Guid.Empty || !knownIds.Contains(id.Value))
        {
            errors.Add(Error(property, code, $"No local catalog definition has UUID {id}."));
        }
    }

    private static FieldMutationError? FindReferences(SqliteConnection connection, SqliteTransaction transaction,
        Func<Model.Field, bool> predicate, string property, string code, string message)
    {
        List<Guid> fieldIds = ReadFields(connection, transaction)
            .Where(pair => predicate(pair.Value))
            .Select(pair => pair.Key)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        return fieldIds.Count == 0
            ? null
            : new FieldMutationError { Property = property, Code = code, Message = message, ReferencingFieldIDs = fieldIds };
    }

    private static Dictionary<Guid, Model.Field> ReadFields(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<Guid, Model.Field> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ID, Field FROM FieldTable";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Model.Field? field = JsonSerializer.Deserialize<Model.Field>(reader.GetString(1), JsonSettings.Options);
            if (field != null)
            {
                result[reader.GetGuid(0)] = field;
            }
        }
        return result;
    }

    private static HashSet<Guid> ReadDefinitionIds<T>(SqliteConnection connection, SqliteTransaction transaction,
        string table, string column, Func<T, Guid?> idSelector)
    {
        HashSet<Guid> result = [];
        foreach (T value in ReadDocuments<T>(connection, transaction, table, column))
        {
            if (idSelector(value) is Guid id && id != Guid.Empty)
            {
                result.Add(id);
            }
        }
        return result;
    }

    private static Dictionary<Guid, HashSet<Guid>> ReadCategoryOptions<TCategory, TOption>(
        SqliteConnection connection, SqliteTransaction transaction, string table, string column,
        Func<TCategory, Guid?> categoryId, Func<TCategory, List<TOption>?> options,
        Func<TOption, Guid> optionId)
    {
        Dictionary<Guid, HashSet<Guid>> result = [];
        foreach (TCategory category in ReadDocuments<TCategory>(connection, transaction, table, column))
        {
            if (categoryId(category) is not Guid id || id == Guid.Empty)
            {
                continue;
            }
            result[id] = (options(category) ?? []).Select(optionId).Where(value => value != Guid.Empty).ToHashSet();
        }
        return result;
    }

    private static List<T> ReadDocuments<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        List<T> result = [];
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table}";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            T? value = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
            if (value != null)
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static FieldMutationError Error(string property, string code, string message) =>
        new() { Property = property, Code = code, Message = message };
}
