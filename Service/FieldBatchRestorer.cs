using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.Drilling.Field.Model;
using FieldModel = OSDC.Drilling.Field.Model.Field;

namespace OSDC.Drilling.Field.Service;

public enum FieldBatchRestoreFailureKind
{
    None = 0,
    InvalidRequest = 1,
    Conflict = 2,
    StorageFailure = 3
}

public sealed class FieldBatchRestoreOutcome
{
    public FieldBatchRestoreResponse? Response { get; init; }
    public FieldBatchErrorEnvelope? Error { get; init; }
    public FieldBatchRestoreFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response != null && FailureKind == FieldBatchRestoreFailureKind.None;
}

/// <summary>
/// Validates and writes a complete field batch in one SQLite transaction.
/// </summary>
public static class FieldBatchRestorer
{
    public static FieldBatchRestoreOutcome Restore(
        SqliteConnection connection,
        FieldBatchRestoreRequest? request,
        DateTimeOffset restoredAtUtc)
    {
        List<FieldBatchError> validationErrors = Validate(request);
        if (validationErrors.Count != 0)
        {
            return Failure(
                FieldBatchRestoreFailureKind.InvalidRequest,
                "invalid_batch_restore_request",
                "The field batch-restore request is invalid.",
                validationErrors);
        }

        // Serialize the complete batch before the write transaction. This both
        // proves that every record can be persisted and prevents a late
        // serialization failure from following earlier writes.
        List<PreparedField> prepared = [];
        try
        {
            foreach (FieldModel field in request!.Document!.Fields)
            {
                prepared.Add(new PreparedField(
                    field.MetaInfo!.ID,
                    JsonSerializer.Serialize(field.MetaInfo, JsonSettings.Options),
                    JsonSerializer.Serialize(field, JsonSettings.Options)));
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return StorageFailure("One or more fields could not be serialized for restore.");
        }

        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            List<bool> exists = [];
            for (int index = 0; index < prepared.Count; index++)
            {
                PreparedField field = prepared[index];
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "SELECT COUNT(*) FROM FieldTable WHERE ID = $id";
                command.Parameters.AddWithValue("$id", field.ID.ToString());
                exists.Add(Convert.ToInt64(command.ExecuteScalar()) != 0);
            }

            if (request.ConflictPolicy == FieldBatchRestoreConflictPolicy.FailIfExists)
            {
                List<FieldBatchError> conflicts = [];
                for (int index = 0; index < prepared.Count; index++)
                {
                    if (exists[index])
                    {
                        conflicts.Add(Error(index, "Document.Fields", "field_already_exists",
                            $"A stored field already has UUID '{prepared[index].ID}'."));
                    }
                }

                if (conflicts.Count != 0)
                {
                    transaction.Rollback();
                    return Failure(
                        FieldBatchRestoreFailureKind.Conflict,
                        "field_restore_conflict",
                        "No fields were restored because one or more UUIDs already exist.",
                        conflicts);
                }
            }

            for (int index = 0; index < prepared.Count; index++)
            {
                PreparedField field = prepared[index];
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = request.ConflictPolicy == FieldBatchRestoreConflictPolicy.ReplaceExisting
                    ? "INSERT INTO FieldTable (ID, MetaInfo, Field) VALUES ($id, $metaInfo, $field) " +
                      "ON CONFLICT(ID) DO UPDATE SET MetaInfo = excluded.MetaInfo, Field = excluded.Field"
                    : "INSERT INTO FieldTable (ID, MetaInfo, Field) VALUES ($id, $metaInfo, $field)";
                command.Parameters.AddWithValue("$id", field.ID.ToString());
                command.Parameters.AddWithValue("$metaInfo", field.MetaInfoJson);
                command.Parameters.AddWithValue("$field", field.FieldJson);
                if (command.ExecuteNonQuery() != 1)
                {
                    throw new SqliteException($"Unexpected affected-row count while restoring field {field.ID}.", 1);
                }
            }

            transaction.Commit();
            return new FieldBatchRestoreOutcome
            {
                Response = new FieldBatchRestoreResponse
                {
                    RestoredAtUtc = restoredAtUtc.ToUniversalTime(),
                    CreatedCount = exists.Count(value => !value),
                    ReplacedCount = exists.Count(value => value),
                    FieldIDs = prepared.Select(field => field.ID).ToList()
                }
            };
        }
        catch (SqliteException)
        {
            try
            {
                transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
                // The transaction may already have been rolled back by SQLite.
            }
            return StorageFailure("The field database rejected the batch. No fields were restored.");
        }
    }

    public static FieldBatchRestoreOutcome StorageFailure(string message)
    {
        return Failure(
            FieldBatchRestoreFailureKind.StorageFailure,
            "field_restore_failed",
            message,
            [Error(null, "Document.Fields", "storage_failure", "The complete restore transaction was rolled back.")]);
    }

    private static List<FieldBatchError> Validate(FieldBatchRestoreRequest? request)
    {
        if (request == null)
        {
            return [Error(null, "Request", "required", "A batch-restore request is required.")];
        }

        List<FieldBatchError> errors = [];
        if (request.ConflictPolicy is not FieldBatchRestoreConflictPolicy.FailIfExists and not FieldBatchRestoreConflictPolicy.ReplaceExisting)
        {
            errors.Add(Error(null, "ConflictPolicy", "invalid_conflict_policy", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        }

        FieldBatchExportDocument? document = request.Document;
        if (document == null)
        {
            errors.Add(Error(null, "Document", "required", "A batch-export document is required."));
            return errors;
        }
        if (!string.Equals(document.FormatIdentifier, FieldBatchExportDocument.CurrentFormatIdentifier, StringComparison.Ordinal))
        {
            errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format",
                $"FormatIdentifier must be '{FieldBatchExportDocument.CurrentFormatIdentifier}'."));
        }
        if (document.SchemaVersion != FieldBatchExportDocument.CurrentSchemaVersion)
        {
            errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version",
                $"SchemaVersion must be {FieldBatchExportDocument.CurrentSchemaVersion}."));
        }
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add(Error(null, "Document.ExportedAtUtc", "invalid_export_timestamp",
                "ExportedAtUtc must be a non-default UTC timestamp with offset +00:00."));
        }
        if (document.Fields == null || document.Fields.Count == 0)
        {
            errors.Add(Error(null, "Document.Fields", "required", "At least one field is required for restore."));
            return errors;
        }

        var positionsById = new Dictionary<Guid, int>();
        for (int index = 0; index < document.Fields.Count; index++)
        {
            FieldModel? field = document.Fields[index];
            Guid? id = field?.MetaInfo?.ID;
            if (field == null)
            {
                errors.Add(Error(index, "Document.Fields", "null_field", "A restored field must not be null."));
                continue;
            }
            if (id == null || id == Guid.Empty)
            {
                errors.Add(Error(index, "Document.Fields.MetaInfo.ID", "empty_uuid", "Every restored field must have a non-empty UUID."));
            }
            else if (positionsById.TryGetValue(id.Value, out int firstIndex))
            {
                errors.Add(Error(index, "Document.Fields.MetaInfo.ID", "duplicate_uuid",
                    $"Field UUID '{id}' duplicates position {firstIndex}."));
            }
            else
            {
                positionsById.Add(id.Value, index);
            }

            if (field.ProjectionDefinitionID == Guid.Empty)
            {
                errors.Add(Error(index, "Document.Fields.ProjectionDefinitionID", "empty_uuid",
                    "ProjectionDefinitionID must be omitted or a non-empty UUID."));
            }
        }
        return errors;
    }

    private static FieldBatchRestoreOutcome Failure(
        FieldBatchRestoreFailureKind kind,
        string error,
        string message,
        List<FieldBatchError> errors)
    {
        return new FieldBatchRestoreOutcome
        {
            FailureKind = kind,
            Error = new FieldBatchErrorEnvelope { Error = error, Message = message, Errors = errors }
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

    private sealed record PreparedField(Guid ID, string MetaInfoJson, string FieldJson);
}
