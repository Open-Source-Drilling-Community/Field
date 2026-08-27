using Microsoft.Data.Sqlite;
using NORCE.Drilling.Field.Service.Managers;
using NUnit.Framework;

namespace NORCE.Drilling.Field.ServiceMcpTest;

[TestFixture]
public sealed class FieldProjectionReferenceMigratorTests
{
    [Test]
    public void Missing_mapping_fails_before_changing_any_field()
    {
        using SqliteConnection connection = CreateDatabase(out Guid fieldId, out Guid legacyId);

        var error = Assert.Throws<InvalidOperationException>(() =>
            FieldProjectionReferenceMigrator.Plan(connection, new Dictionary<Guid, Guid>()));

        Assert.That(error!.Message, Does.Contain(fieldId.ToString()));
        Assert.That(ReadFieldJson(connection), Does.Contain(legacyId.ToString()));
        Assert.That(ReadFieldJson(connection), Does.Not.Contain("ProjectionDefinitionID"));
    }

    [Test]
    public void Reviewed_mapping_is_applied_atomically_and_preserves_other_data()
    {
        using SqliteConnection connection = CreateDatabase(out _, out Guid legacyId);
        Guid replacementId = Guid.NewGuid();
        FieldProjectionMigrationPlan plan = FieldProjectionReferenceMigrator.Plan(
            connection,
            new Dictionary<Guid, Guid> { [legacyId] = replacementId });

        using (SqliteTransaction transaction = connection.BeginTransaction())
        {
            FieldProjectionReferenceMigrator.Apply(connection, transaction, plan);
            transaction.Commit();
        }

        string migrated = ReadFieldJson(connection);
        Assert.Multiple(() =>
        {
            Assert.That(plan.Changes, Has.Count.EqualTo(1));
            Assert.That(migrated, Does.Contain($"\"ProjectionDefinitionID\":\"{replacementId}\""));
            Assert.That(migrated, Does.Not.Contain("CartographicProjectionID"));
            Assert.That(migrated, Does.Contain("\"Name\":\"Preserved field\""));
        });
    }

    [Test]
    public void Null_legacy_reference_does_not_erase_an_existing_new_reference()
    {
        Guid fieldId = Guid.NewGuid();
        Guid currentId = Guid.NewGuid();
        using SqliteConnection connection = CreateDatabase(
            fieldId,
            $"{{\"MetaInfo\":{{\"ID\":\"{fieldId}\"}},\"CartographicProjectionID\":null,\"ProjectionDefinitionID\":\"{currentId}\"}}");

        FieldProjectionMigrationPlan plan = FieldProjectionReferenceMigrator.Plan(connection, new Dictionary<Guid, Guid>());
        using SqliteTransaction transaction = connection.BeginTransaction();
        FieldProjectionReferenceMigrator.Apply(connection, transaction, plan);
        transaction.Commit();

        string migrated = ReadFieldJson(connection);
        Assert.That(migrated, Does.Contain($"\"ProjectionDefinitionID\":\"{currentId}\""));
        Assert.That(migrated, Does.Not.Contain("CartographicProjectionID"));
    }

    [Test]
    public void Apply_rejects_a_field_changed_after_planning()
    {
        using SqliteConnection connection = CreateDatabase(out _, out Guid legacyId);
        FieldProjectionMigrationPlan plan = FieldProjectionReferenceMigrator.Plan(
            connection,
            new Dictionary<Guid, Guid> { [legacyId] = Guid.NewGuid() });

        using (SqliteCommand concurrentUpdate = connection.CreateCommand())
        {
            concurrentUpdate.CommandText = "UPDATE FieldTable SET Field = replace(Field, 'Preserved field', 'Changed field')";
            concurrentUpdate.ExecuteNonQuery();
        }

        using SqliteTransaction transaction = connection.BeginTransaction();
        Assert.Throws<InvalidOperationException>(() =>
            FieldProjectionReferenceMigrator.Apply(connection, transaction, plan));
        transaction.Rollback();
        Assert.That(ReadFieldJson(connection), Does.Contain("Changed field"));
    }

    private static SqliteConnection CreateDatabase(out Guid fieldId, out Guid legacyId)
    {
        fieldId = Guid.NewGuid();
        legacyId = Guid.NewGuid();
        return CreateDatabase(fieldId, $"{{\"MetaInfo\":{{\"ID\":\"{fieldId}\"}},\"Name\":\"Preserved field\",\"CartographicProjectionID\":\"{legacyId}\"}}");
    }

    private static SqliteConnection CreateDatabase(Guid fieldId, string fieldJson)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE FieldTable (ID text primary key, MetaInfo text, Field text)";
        create.ExecuteNonQuery();
        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO FieldTable (ID, MetaInfo, Field) VALUES ($id, '{}', $field)";
        insert.Parameters.AddWithValue("$id", fieldId.ToString());
        insert.Parameters.AddWithValue("$field", fieldJson);
        insert.ExecuteNonQuery();
        return connection;
    }

    private static string ReadFieldJson(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Field FROM FieldTable";
        return (string)command.ExecuteScalar()!;
    }
}
