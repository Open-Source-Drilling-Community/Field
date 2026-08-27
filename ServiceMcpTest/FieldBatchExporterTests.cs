using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.Service;
using NUnit.Framework;
using FieldModel = OSDC.Drilling.Field.Model.Field;

namespace OSDC.Drilling.Field.ServiceMcpTest;

[TestFixture]
public sealed class FieldBatchExporterTests
{
    [Test]
    public void Selected_export_preserves_requested_order_and_writes_versioned_envelope()
    {
        FieldModel first = CreateField(Guid.Parse("00000000-0000-0000-0000-000000000001"), "First");
        FieldModel second = CreateField(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Second");
        DateTimeOffset exportedAt = DateTimeOffset.Parse("2026-08-27T12:34:56+02:00");

        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest
            {
                Scope = FieldBatchExportScope.Selected,
                FieldIDs = [second.MetaInfo!.ID, first.MetaInfo!.ID]
            },
            [first, second],
            exportedAt);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Error, Is.Null);
            Assert.That(outcome.Document!.FormatIdentifier, Is.EqualTo(FieldBatchExportDocument.CurrentFormatIdentifier));
            Assert.That(outcome.Document.SchemaVersion, Is.EqualTo(FieldBatchExportDocument.CurrentSchemaVersion));
            Assert.That(outcome.Document.ExportedAtUtc, Is.EqualTo(exportedAt.ToUniversalTime()));
            Assert.That(outcome.Document.Fields.Select(field => field.MetaInfo!.ID),
                Is.EqualTo(new[] { second.MetaInfo!.ID, first.MetaInfo!.ID }));
        });
    }

    [Test]
    public void All_export_is_deterministically_ordered_by_uuid()
    {
        FieldModel first = CreateField(Guid.Parse("00000000-0000-0000-0000-000000000001"), "First");
        FieldModel second = CreateField(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Second");

        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest { Scope = FieldBatchExportScope.All },
            [second, first],
            DateTimeOffset.UtcNow);

        Assert.That(outcome.Document!.Fields.Select(field => field.MetaInfo!.ID),
            Is.EqualTo(new[] { first.MetaInfo!.ID, second.MetaInfo!.ID }));
    }

    [Test]
    public void Invalid_selected_ids_reject_the_complete_export_with_indexed_errors()
    {
        Guid repeated = Guid.NewGuid();
        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest
            {
                Scope = FieldBatchExportScope.Selected,
                FieldIDs = [Guid.Empty, repeated, repeated]
            },
            [],
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Document, Is.Null);
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchExportFailureKind.InvalidRequest));
            Assert.That(outcome.Error!.Error, Is.EqualTo("invalid_batch_export_request"));
            Assert.That(outcome.Error.Errors.Select(error => error.PositionIndex), Is.EqualTo(new int?[] { 0, 2 }));
            Assert.That(outcome.Error.Errors.Select(error => error.Code), Is.EqualTo(new[] { "empty_uuid", "duplicate_uuid" }));
        });
    }

    [Test]
    public void Missing_selected_field_rejects_the_complete_export()
    {
        FieldModel existing = CreateField(Guid.NewGuid(), "Existing");
        Guid missing = Guid.NewGuid();
        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest
            {
                Scope = FieldBatchExportScope.Selected,
                FieldIDs = [existing.MetaInfo!.ID, missing]
            },
            [existing],
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Document, Is.Null);
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchExportFailureKind.FieldNotFound));
            Assert.That(outcome.Error!.Error, Is.EqualTo("field_not_found"));
            Assert.That(outcome.Error.Errors, Has.Count.EqualTo(1));
            Assert.That(outcome.Error.Errors[0].PositionIndex, Is.EqualTo(1));
            Assert.That(outcome.Error.Errors[0].Code, Is.EqualTo("field_not_found"));
        });
    }

    [TestCase(FieldBatchExportScope.Unspecified)]
    [TestCase((FieldBatchExportScope)99)]
    public void Invalid_scope_is_rejected(FieldBatchExportScope scope)
    {
        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest { Scope = scope },
            [],
            DateTimeOffset.UtcNow);

        Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchExportFailureKind.InvalidRequest));
        Assert.That(outcome.Error!.Errors.Single().Code, Is.EqualTo("invalid_scope"));
    }

    [Test]
    public void Corrupt_stored_field_rejects_the_complete_export()
    {
        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new FieldBatchExportRequest { Scope = FieldBatchExportScope.All },
            [new FieldModel()],
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Document, Is.Null);
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchExportFailureKind.StorageFailure));
            Assert.That(outcome.Error!.Errors.Single().Code, Is.EqualTo("invalid_stored_field"));
        });
    }

    private static FieldModel CreateField(Guid id, string name)
    {
        return new FieldModel
        {
            MetaInfo = new MetaInfo { ID = id },
            Name = name
        };
    }
}
