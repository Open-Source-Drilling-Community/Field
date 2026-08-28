using System.Text.Json;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.Service;
using FieldModel = OSDC.Drilling.Field.Model.Field;

namespace OSDC.Drilling.Field.ServiceMcpTest;

[TestFixture]
public sealed class FieldBatchRestorerTests
{
    [Test]
    public void Invalid_document_is_rejected_before_any_write()
    {
        using SqliteConnection connection = CreateDatabase();
        Guid duplicateId = Guid.NewGuid();
        var request = Request(
            FieldBatchRestoreConflictPolicy.FailIfExists,
            CreateField(duplicateId, "First"),
            CreateField(duplicateId, "Duplicate"));
        request.Document!.FormatIdentifier = "Some.Other.Format";
        request.Document.SchemaVersion = 99;

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchRestoreFailureKind.InvalidRequest));
            Assert.That(outcome.Response, Is.Null);
            Assert.That(outcome.Error!.Errors.Select(error => error.Code),
                Is.EqualTo(new[] { "unsupported_format", "unsupported_schema_version", "duplicate_uuid" }));
            Assert.That(CountFields(connection), Is.Zero);
        });
    }

    [Test]
    public void Fail_if_exists_reports_every_conflict_and_changes_nothing()
    {
        using SqliteConnection connection = CreateDatabase();
        Guid existingId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        Insert(connection, CreateField(existingId, "Original"));

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(
            connection,
            Request(
                FieldBatchRestoreConflictPolicy.FailIfExists,
                CreateField(newId, "Would be new"),
                CreateField(existingId, "Would replace")),
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchRestoreFailureKind.Conflict));
            Assert.That(outcome.Error!.Error, Is.EqualTo("field_restore_conflict"));
            Assert.That(outcome.Error.Errors.Single().PositionIndex, Is.EqualTo(1));
            Assert.That(outcome.Error.Errors.Single().Code, Is.EqualTo("field_already_exists"));
            Assert.That(CountFields(connection), Is.EqualTo(1));
            Assert.That(ReadName(connection, existingId), Is.EqualTo("Original"));
            Assert.That(Contains(connection, newId), Is.False);
        });
    }

    [Test]
    public void Replace_existing_commits_new_and_replaced_fields_together()
    {
        using SqliteConnection connection = CreateDatabase();
        Guid existingId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        Insert(connection, CreateField(existingId, "Original"));
        DateTimeOffset restoredAt = DateTimeOffset.Parse("2026-08-27T15:45:00+02:00");

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(
            connection,
            Request(
                FieldBatchRestoreConflictPolicy.ReplaceExisting,
                CreateField(newId, "New"),
                CreateField(existingId, "Replaced")),
            restoredAt);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Response!.CreatedCount, Is.EqualTo(1));
            Assert.That(outcome.Response.ReplacedCount, Is.EqualTo(1));
            Assert.That(outcome.Response.RestoredAtUtc, Is.EqualTo(restoredAt.ToUniversalTime()));
            Assert.That(outcome.Response.FieldIDs, Is.EqualTo(new[] { newId, existingId }));
            Assert.That(CountFields(connection), Is.EqualTo(2));
            Assert.That(ReadName(connection, newId), Is.EqualTo("New"));
            Assert.That(ReadName(connection, existingId), Is.EqualTo("Replaced"));
        });
    }

    [Test]
    public void Storage_failure_after_an_earlier_insert_rolls_back_the_complete_batch()
    {
        using SqliteConnection connection = CreateDatabase();
        Guid firstId = Guid.NewGuid();
        Guid rejectedId = Guid.NewGuid();
        using (SqliteCommand trigger = connection.CreateCommand())
        {
            trigger.CommandText = $"CREATE TRIGGER reject_restore BEFORE INSERT ON FieldTable " +
                $"WHEN NEW.ID = '{rejectedId}' BEGIN SELECT RAISE(ABORT, 'forced failure'); END";
            trigger.ExecuteNonQuery();
        }

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(
            connection,
            Request(
                FieldBatchRestoreConflictPolicy.ReplaceExisting,
                CreateField(firstId, "Inserted first"),
                CreateField(rejectedId, "Rejected second")),
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchRestoreFailureKind.StorageFailure));
            Assert.That(outcome.Error!.Error, Is.EqualTo("field_restore_failed"));
            Assert.That(CountFields(connection), Is.Zero);
        });
    }

    [Test]
    public void Empty_projection_uuid_is_rejected_with_its_field_position()
    {
        using SqliteConnection connection = CreateDatabase();
        FieldModel field = CreateField(Guid.NewGuid(), "Invalid projection");
        field.ProjectionDefinitionID = Guid.Empty;

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(
            connection,
            Request(FieldBatchRestoreConflictPolicy.FailIfExists, field),
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchRestoreFailureKind.InvalidRequest));
            Assert.That(outcome.Error!.Errors.Single().PositionIndex, Is.Zero);
            Assert.That(outcome.Error.Errors.Single().Property, Is.EqualTo("Document.Fields.ProjectionDefinitionID"));
            Assert.That(CountFields(connection), Is.Zero);
        });
    }

    private static FieldBatchRestoreRequest Request(
        FieldBatchRestoreConflictPolicy policy,
        params FieldModel[] fields)
    {
        return new FieldBatchRestoreRequest
        {
            ConflictPolicy = policy,
            Document = new FieldBatchExportDocument
            {
                SchemaVersion = FieldBatchExportDocument.LegacySchemaVersion,
                ExportedAtUtc = DateTimeOffset.UtcNow,
                Fields = fields.ToList()
            }
        };
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE FieldTable (ID text primary key, MetaInfo text, Field text);
            CREATE TABLE FieldFeatureCategoryTable (ID text primary key, FieldFeatureCategory text);
            CREATE TABLE FieldMembershipCategoryTable (ID text primary key, FieldMembershipCategory text);
            CREATE TABLE FieldIdentityTable (ID text primary key, FieldIdentity text);
            CREATE TABLE FieldDelineationLineTypeTable (ID text primary key, FieldDelineationLineType text);
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static FieldModel CreateField(Guid id, string name)
    {
        return new FieldModel { MetaInfo = new MetaInfo { ID = id }, Name = name };
    }

    private static void Insert(SqliteConnection connection, FieldModel field)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldTable (ID, MetaInfo, Field) VALUES ($id, $metaInfo, $field)";
        command.Parameters.AddWithValue("$id", field.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("$metaInfo", JsonSerializer.Serialize(field.MetaInfo));
        command.Parameters.AddWithValue("$field", JsonSerializer.Serialize(field));
        command.ExecuteNonQuery();
    }

    private static long CountFields(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM FieldTable";
        return (long)command.ExecuteScalar()!;
    }

    private static bool Contains(SqliteConnection connection, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM FieldTable WHERE ID = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return (long)command.ExecuteScalar()! != 0;
    }

    private static string? ReadName(SqliteConnection connection, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Field FROM FieldTable WHERE ID = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        string json = (string)command.ExecuteScalar()!;
        return JsonSerializer.Deserialize<FieldModel>(json)?.Name;
    }
}
