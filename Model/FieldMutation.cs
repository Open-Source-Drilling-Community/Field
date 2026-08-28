using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Model;

/// <summary>
/// Stable error envelope for Field and locally owned catalog mutations.
/// </summary>
public sealed class FieldMutationErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<FieldMutationError> Errors { get; set; } = [];
}

/// <summary>
/// Identifies an invalid reference, an active dependent reference, or a stale
/// optimistic-concurrency token.
/// </summary>
public sealed class FieldMutationError
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<Guid> ReferencingFieldIDs { get; set; } = [];
}
