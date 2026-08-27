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
    public static FieldBatchExportOutcome Create(
        FieldBatchExportRequest? request,
        IEnumerable<Model.Field?> snapshot,
        DateTimeOffset exportedAtUtc)
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

        return new FieldBatchExportOutcome
        {
            Document = new FieldBatchExportDocument
            {
                ExportedAtUtc = exportedAtUtc.ToUniversalTime(),
                Fields = exportedFields
            }
        };
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
