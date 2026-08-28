using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Field.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSDC.Drilling.Field.Service.Managers;

internal static class FieldCatalogMutationManager
{
    public static FieldMutationResult UpdateFeatureCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, FieldFeatureCategory? value) =>
        UpdateCategory(manager, logger, id, expectedModifiedUtc, value,
            "FieldFeatureCategoryTable", "FieldFeatureCategory",
            category => category.MetaInfo, category => category.CreationDate, (category, date) => category.CreationDate = date,
            category => category.LastModificationDate, (category, date) => category.LastModificationDate = date,
            category => category.Options, option => option.ID, (option, optionId) => option.ID = optionId,
            (connection, transaction, categoryId, options) => FieldReferenceIntegrityValidator.FindFeatureCategoryReferences(connection, transaction, categoryId, options),
            (command, category) =>
            {
                command.CommandText = "UPDATE FieldFeatureCategoryTable SET MetaInfo=$meta, Name=$name, IsExclusive=$exclusive, HasValidityPeriod=$validity, CreationDate=$created, LastModificationDate=$modified, FieldFeatureCategory=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", category.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$exclusive", category.IsExclusive ? 1 : 0);
                command.Parameters.AddWithValue("$validity", category.HasValidityPeriod ? 1 : 0);
            });

    public static FieldMutationResult UpdateMembershipCategory(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, FieldMembershipCategory? value) =>
        UpdateCategory(manager, logger, id, expectedModifiedUtc, value,
            "FieldMembershipCategoryTable", "FieldMembershipCategory",
            category => category.MetaInfo, category => category.CreationDate, (category, date) => category.CreationDate = date,
            category => category.LastModificationDate, (category, date) => category.LastModificationDate = date,
            category => category.Options, option => option.ID, (option, optionId) => option.ID = optionId,
            (connection, transaction, categoryId, options) => FieldReferenceIntegrityValidator.FindMembershipCategoryReferences(connection, transaction, categoryId, options),
            (command, category) =>
            {
                command.CommandText = "UPDATE FieldMembershipCategoryTable SET MetaInfo=$meta, Name=$name, IsExclusive=$exclusive, HasValidityPeriod=$validity, CreationDate=$created, LastModificationDate=$modified, FieldMembershipCategory=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", category.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$exclusive", category.IsExclusive ? 1 : 0);
                command.Parameters.AddWithValue("$validity", category.HasValidityPeriod ? 1 : 0);
            });

    public static FieldMutationResult UpdateIdentity(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, FieldIdentity? value) =>
        UpdateNamed(manager, logger, id, expectedModifiedUtc, value,
            "FieldIdentityTable", "FieldIdentity", identity => identity.MetaInfo,
            identity => identity.CreationDate, (identity, date) => identity.CreationDate = date,
            identity => identity.LastModificationDate, (identity, date) => identity.LastModificationDate = date,
            (command, identity) =>
            {
                command.CommandText = "UPDATE FieldIdentityTable SET MetaInfo=$meta, Name=$name, CreationDate=$created, LastModificationDate=$modified, FieldIdentity=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", identity.Name ?? (object)DBNull.Value);
            });

    public static FieldMutationResult UpdateDelineationLineType(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, FieldDelineationLineType? value) =>
        UpdateNamed(manager, logger, id, expectedModifiedUtc, value,
            "FieldDelineationLineTypeTable", "FieldDelineationLineType", lineType => lineType.MetaInfo,
            lineType => lineType.CreationDate, (lineType, date) => lineType.CreationDate = date,
            lineType => lineType.LastModificationDate, (lineType, date) => lineType.LastModificationDate = date,
            (command, lineType) =>
            {
                command.CommandText = "UPDATE FieldDelineationLineTypeTable SET MetaInfo=$meta, Name=$name, CreationDate=$created, LastModificationDate=$modified, FieldDelineationLineType=$document WHERE ID=$id";
                command.Parameters.AddWithValue("$name", lineType.Name ?? (object)DBNull.Value);
            });

    public static FieldMutationResult DeleteFeatureCategory(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "FieldFeatureCategoryTable",
            (connection, transaction) => FieldReferenceIntegrityValidator.FindFeatureCategoryReferences(connection, transaction, id));

    public static FieldMutationResult DeleteMembershipCategory(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "FieldMembershipCategoryTable",
            (connection, transaction) => FieldReferenceIntegrityValidator.FindMembershipCategoryReferences(connection, transaction, id));

    public static FieldMutationResult DeleteIdentity(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "FieldIdentityTable",
            (connection, transaction) => FieldReferenceIntegrityValidator.FindIdentityReferences(connection, transaction, id));

    public static FieldMutationResult DeleteDelineationLineType(SqlConnectionManager manager, ILogger logger, Guid id) =>
        Delete(manager, logger, id, "FieldDelineationLineTypeTable",
            (connection, transaction) => FieldReferenceIntegrityValidator.FindDelineationLineTypeReferences(connection, transaction, id));

    private static FieldMutationResult UpdateCategory<TCategory, TOption>(SqlConnectionManager manager, ILogger logger,
        Guid id, DateTimeOffset expectedModifiedUtc, TCategory? value, string table, string documentColumn,
        Func<TCategory, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<TCategory, DateTimeOffset?> creationDate, Action<TCategory, DateTimeOffset?> setCreationDate,
        Func<TCategory, DateTimeOffset?> modificationDate, Action<TCategory, DateTimeOffset?> setModificationDate,
        Func<TCategory, List<TOption>?> options, Func<TOption, Guid> optionId, Action<TOption, Guid> setOptionId,
        Func<SqliteConnection, SqliteTransaction, Guid, IReadOnlyCollection<Guid>, FieldMutationError?> findRemovedReferences,
        Action<SqliteCommand, TCategory> configure)
        where TCategory : class
    {
        if (value == null || meta(value)?.ID != id || id == Guid.Empty)
        {
            return FieldMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.");
        }
        List<TOption> categoryOptions = options(value) ?? [];
        foreach (TOption option in categoryOptions.Where(option => optionId(option) == Guid.Empty))
        {
            setOptionId(option, Guid.NewGuid());
        }
        List<Guid> optionIds = categoryOptions.Select(optionId).ToList();
        if (optionIds.Count != optionIds.Distinct().Count())
        {
            return FieldMutationResult.Invalid("Options", "duplicate_option_id", "Option UUIDs must be unique within a category.");
        }

        return ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, table, documentColumn,
            meta, creationDate, setCreationDate, modificationDate, setModificationDate,
            (connection, transaction) => findRemovedReferences(connection, transaction, id, optionIds), configure);
    }

    private static FieldMutationResult UpdateNamed<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, T? value, string table, string documentColumn,
        Func<T, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<T, DateTimeOffset?> creationDate, Action<T, DateTimeOffset?> setCreationDate,
        Func<T, DateTimeOffset?> modificationDate, Action<T, DateTimeOffset?> setModificationDate,
        Action<SqliteCommand, T> configure)
        where T : class =>
        value == null || meta(value)?.ID != id || id == Guid.Empty
            ? FieldMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must match MetaInfo.ID.")
            : ExecuteUpdate(manager, logger, id, expectedModifiedUtc, value, table, documentColumn,
                meta, creationDate, setCreationDate, modificationDate, setModificationDate, null, configure);

    private static FieldMutationResult ExecuteUpdate<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, T value, string table, string documentColumn,
        Func<T, OSDC.DotnetLibraries.General.DataManagement.MetaInfo?> meta,
        Func<T, DateTimeOffset?> creationDate, Action<T, DateTimeOffset?> setCreationDate,
        Func<T, DateTimeOffset?> modificationDate, Action<T, DateTimeOffset?> setModificationDate,
        Func<SqliteConnection, SqliteTransaction, FieldMutationError?>? referenceCheck,
        Action<SqliteCommand, T> configure)
        where T : class
    {
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null)
        {
            return FieldMutationResult.StorageFailure();
        }
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            T? stored = Read<T>(connection, transaction, table, documentColumn, id);
            if (stored == null)
            {
                transaction.Rollback();
                return FieldMutationResult.NotFound("The catalog definition does not exist.");
            }
            DateTimeOffset? storedModified = modificationDate(stored);
            if (storedModified == null || !SameInstant(storedModified.Value, expectedModifiedUtc))
            {
                transaction.Rollback();
                return FieldMutationResult.ConcurrencyConflict("expectedModifiedUtc",
                    $"Expected {expectedModifiedUtc:O}, but the stored definition was modified at {storedModified:O}.");
            }
            FieldMutationError? referenceError = referenceCheck?.Invoke(connection, transaction);
            if (referenceError != null)
            {
                transaction.Rollback();
                return FieldMutationResult.ReferenceConflict(referenceError);
            }

            setCreationDate(value, creationDate(stored));
            setModificationDate(value, DateTimeOffset.UtcNow);
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            configure(command, value);
            command.Parameters.AddWithValue("$id", id.ToString());
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(meta(value), JsonSettings.Options));
            command.Parameters.AddWithValue("$created", creationDate(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$modified", modificationDate(value)?.ToString(SqlConnectionManager.DATE_TIME_FORMAT) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
            if (command.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return FieldMutationResult.StorageFailure();
            }
            transaction.Commit();
            return FieldMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to update {Table} record {RecordId}", table, id);
            return FieldMutationResult.StorageFailure();
        }
    }

    private static FieldMutationResult Delete(SqlConnectionManager manager, ILogger logger, Guid id, string table,
        Func<SqliteConnection, SqliteTransaction, FieldMutationError?> referenceCheck)
    {
        if (id == Guid.Empty)
        {
            return FieldMutationResult.Invalid("id", "invalid_id", "A non-empty UUID is required.");
        }
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null)
        {
            return FieldMutationResult.StorageFailure();
        }
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            using (SqliteCommand exists = connection.CreateCommand())
            {
                exists.Transaction = transaction;
                exists.CommandText = $"SELECT COUNT(*) FROM {table} WHERE ID=$id";
                exists.Parameters.AddWithValue("$id", id.ToString());
                if (Convert.ToInt64(exists.ExecuteScalar()) == 0)
                {
                    transaction.Rollback();
                    return FieldMutationResult.NotFound("The catalog definition does not exist.");
                }
            }
            FieldMutationError? referenceError = referenceCheck(connection, transaction);
            if (referenceError != null)
            {
                transaction.Rollback();
                return FieldMutationResult.ReferenceConflict(referenceError);
            }
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {table} WHERE ID=$id";
            command.Parameters.AddWithValue("$id", id.ToString());
            if (command.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return FieldMutationResult.StorageFailure();
            }
            transaction.Commit();
            return FieldMutationResult.Success();
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            transaction.Rollback();
            logger.LogError(ex, "Unable to delete {Table} record {RecordId}", table, id);
            return FieldMutationResult.StorageFailure();
        }
    }

    private static T? Read<T>(SqliteConnection connection, SqliteTransaction transaction, string table, string column, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {column} FROM {table} WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<T>(json, JsonSettings.Options) : default;
    }

    private static bool SameInstant(DateTimeOffset left, DateTimeOffset right) => left.UtcTicks == right.UtcTicks;
}
