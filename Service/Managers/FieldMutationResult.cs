using OSDC.Drilling.Field.Model;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Service.Managers;

internal enum FieldMutationFailureKind
{
    None,
    InvalidRequest,
    NotFound,
    Conflict,
    StorageFailure
}

internal sealed record FieldMutationResult(
    FieldMutationFailureKind FailureKind,
    FieldMutationErrorEnvelope? Error = null)
{
    public bool Succeeded => FailureKind == FieldMutationFailureKind.None;

    public static FieldMutationResult Success() => new(FieldMutationFailureKind.None);

    public static FieldMutationResult Invalid(string property, string code, string message) =>
        Failure(FieldMutationFailureKind.InvalidRequest, "invalid_request", "The mutation request is invalid.", property, code, message);

    public static FieldMutationResult NotFound(string message) =>
        new(FieldMutationFailureKind.NotFound, new FieldMutationErrorEnvelope
        {
            Error = "not_found",
            Message = message
        });

    public static FieldMutationResult ConcurrencyConflict(string property, string message) =>
        Failure(FieldMutationFailureKind.Conflict, "concurrency_conflict", "The resource was modified by another caller.",
            property, "concurrency_conflict", message);

    public static FieldMutationResult ReferenceConflict(FieldMutationError error) =>
        new(FieldMutationFailureKind.Conflict, new FieldMutationErrorEnvelope
        {
            Error = "reference_conflict",
            Message = "The mutation would break a Field-owned catalog reference.",
            Errors = [error]
        });

    public static FieldMutationResult InvalidReferences(List<FieldMutationError> errors) =>
        new(FieldMutationFailureKind.InvalidRequest, new FieldMutationErrorEnvelope
        {
            Error = "invalid_reference",
            Message = "One or more Field-owned catalog references are invalid.",
            Errors = errors
        });

    public static FieldMutationResult StorageFailure() =>
        new(FieldMutationFailureKind.StorageFailure, new FieldMutationErrorEnvelope
        {
            Error = "storage_failure",
            Message = "The mutation could not be committed. No partial change was retained."
        });

    private static FieldMutationResult Failure(FieldMutationFailureKind kind, string error, string summary,
        string property, string code, string message) =>
        new(kind, new FieldMutationErrorEnvelope
        {
            Error = error,
            Message = summary,
            Errors = [new FieldMutationError { Property = property, Code = code, Message = message }]
        });
}
