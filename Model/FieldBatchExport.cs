using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Model
{
    /// <summary>
    /// Selects whether a field batch export contains every stored field or an
    /// explicitly ordered selection.
    /// </summary>
    public enum FieldBatchExportScope
    {
        Unspecified = 0,
        All = 1,
        Selected = 2
    }

    /// <summary>
    /// Request for a versioned, read-only export of complete field records.
    /// </summary>
    public sealed class FieldBatchExportRequest
    {
        /// <summary>
        /// All exports every stored field in UUID order. Selected exports the
        /// records named by FieldIDs and preserves that list's order.
        /// </summary>
        public FieldBatchExportScope Scope { get; set; }

        /// <summary>
        /// Required for Selected and forbidden for All. Every UUID must be
        /// non-empty, unique and identify an existing field.
        /// </summary>
        public List<Guid>? FieldIDs { get; set; }
    }

    /// <summary>
    /// Portable backup document produced by the field batch-export API.
    /// </summary>
    public sealed class FieldBatchExportDocument
    {
        public const string CurrentFormatIdentifier = "OSDC.Drilling.Field.BatchExport";
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// Stable discriminator used to reject unrelated JSON documents during import.
        /// </summary>
        public string FormatIdentifier { get; set; } = CurrentFormatIdentifier;

        /// <summary>
        /// Version of the batch document envelope. Version 2 embeds complete Field records
        /// and the server-managed catalog definitions referenced by those records.
        /// </summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        /// UTC time at which the database snapshot was exported.
        /// </summary>
        public DateTimeOffset ExportedAtUtc { get; set; }

        /// <summary>
        /// The portable dependency closure for the exported fields. Only referenced
        /// definitions and referenced category options are included.
        /// </summary>
        public FieldBatchCatalogDependencies? CatalogDependencies { get; set; }

        /// <summary>
        /// Complete field records. Selected exports preserve request order; All
        /// exports are ordered by field UUID for deterministic output.
        /// </summary>
        public List<Field> Fields { get; set; } = [];
    }

    /// <summary>
    /// Server-managed definitions required to interpret the references in an export.
    /// Source UUIDs are retained for remapping and are not imposed on the destination.
    /// </summary>
    public sealed class FieldBatchCatalogDependencies
    {
        public List<FieldFeatureCategory> FeatureCategories { get; set; } = [];
        public List<FieldMembershipCategory> MembershipCategories { get; set; } = [];
        public List<FieldIdentity> Identities { get; set; } = [];
        public List<FieldDelineationLineType> DelineationLineTypes { get; set; } = [];
    }

    /// <summary>
    /// Stable error envelope for field batch export and future batch import operations.
    /// </summary>
    public sealed class FieldBatchErrorEnvelope
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<FieldBatchError> Errors { get; set; } = [];
    }

    /// <summary>
    /// Identifies one invalid request position or property.
    /// </summary>
    public sealed class FieldBatchError
    {
        /// <summary>
        /// Zero-based position in FieldIDs when the error concerns one selected field.
        /// </summary>
        public int? PositionIndex { get; set; }

        public string Property { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Controls how an atomic restore handles field UUIDs already present in the database.
    /// </summary>
    public enum FieldBatchRestoreConflictPolicy
    {
        Unspecified = 0,
        FailIfExists = 1,
        ReplaceExisting = 2
    }

    /// <summary>
    /// Controls how source catalog definitions are resolved on the destination server.
    /// </summary>
    public enum FieldBatchCatalogRestorePolicy
    {
        Unspecified = 0,

        /// <summary>Use a compatible local UUID or unique normalized-name match; fail when missing.</summary>
        MapExisting = 1,

        /// <summary>Map compatible definitions and create locally missing definitions/options.</summary>
        MapOrCreateMissing = 2
    }

    /// <summary>
    /// Request to validate and atomically restore a versioned batch-export document.
    /// </summary>
    public sealed class FieldBatchRestoreRequest
    {
        public FieldBatchRestoreConflictPolicy ConflictPolicy { get; set; }

        /// <summary>
        /// Controls whether missing catalog definitions are rejected or created locally.
        /// </summary>
        public FieldBatchCatalogRestorePolicy CatalogPolicy { get; set; }

        public FieldBatchExportDocument? Document { get; set; }
    }

    /// <summary>
    /// Summary returned only after every field in a restore has been committed.
    /// </summary>
    public sealed class FieldBatchRestoreResponse
    {
        public DateTimeOffset RestoredAtUtc { get; set; }
        public int CreatedCount { get; set; }
        public int ReplacedCount { get; set; }
        public int CreatedCatalogDefinitionCount { get; set; }
        public int CreatedCatalogOptionCount { get; set; }

        /// <summary>Every source-to-local catalog UUID translation applied by the restore.</summary>
        public List<FieldBatchCatalogMapping> CatalogMappings { get; set; } = [];

        /// <summary>
        /// Restored field UUIDs in document order.
        /// </summary>
        public List<Guid> FieldIDs { get; set; } = [];
    }

    public sealed class FieldBatchCatalogMapping
    {
        public string Catalog { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid SourceID { get; set; }
        public Guid LocalID { get; set; }
        public string Resolution { get; set; } = string.Empty;
    }
}
