using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.Service;
using OSDC.Drilling.Field.Service.Managers;
using System.Text.Json;
using FieldModel = OSDC.Drilling.Field.Model.Field;

namespace OSDC.Drilling.Field.ServiceMcpTest;

[TestFixture]
public sealed class SqlConnectionManagerSchemaTests
{
    [Test]
    public void Existing_pre_v2_database_is_rejected_without_modification()
    {
        string connectionString = CreateSharedInMemoryConnectionString();
        using SqliteConnection keeper = new(connectionString);
        keeper.Open();
        using (SqliteCommand setup = keeper.CreateCommand())
        {
            setup.CommandText = """
                PRAGMA user_version = 1;
                CREATE TABLE FieldTable (ID text primary key, MetaInfo text, Field text);
                INSERT INTO FieldTable (ID, MetaInfo, Field) VALUES ('preserved', '{}', '{"Name":"Preserved"}');
                """;
            setup.ExecuteNonQuery();
        }

        InvalidOperationException? error = Assert.Throws<InvalidOperationException>(() =>
            new SqlConnectionManager(connectionString, NullLogger<SqlConnectionManager>.Instance));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("schema version 1 is no longer supported"));
            Assert.That(ExecuteScalar<long>(keeper, "PRAGMA user_version"), Is.EqualTo(1));
            Assert.That(ExecuteScalar<long>(keeper, "SELECT COUNT(*) FROM FieldTable"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Empty_database_is_created_at_current_version_and_can_be_reopened()
    {
        string connectionString = CreateSharedInMemoryConnectionString();
        using SqliteConnection keeper = new(connectionString);
        keeper.Open();

        _ = new SqlConnectionManager(connectionString, NullLogger<SqlConnectionManager>.Instance);
        Assert.Multiple(() =>
        {
            Assert.That(ExecuteScalar<long>(keeper, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManager.CURRENT_SCHEMA_VERSION));
            Assert.That(ExecuteScalar<long>(keeper, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'"), Is.EqualTo(5));
        });

        Assert.DoesNotThrow(() =>
            new SqlConnectionManager(connectionString, NullLogger<SqlConnectionManager>.Instance));
    }

    [Test]
    public void Version_2_database_is_upgraded_with_server_owned_timestamps()
    {
        string connectionString = CreateSharedInMemoryConnectionString();
        using SqliteConnection keeper = new(connectionString);
        keeper.Open();
        _ = new SqlConnectionManager(connectionString, NullLogger<SqlConnectionManager>.Instance);

        Guid fieldId = Guid.NewGuid();
        var field = new FieldModel { MetaInfo = new MetaInfo { ID = fieldId }, Name = "Preserved" };
        using (SqliteCommand setup = keeper.CreateCommand())
        {
            setup.CommandText = "PRAGMA user_version = 2; INSERT INTO FieldTable (ID,MetaInfo,Field) VALUES ($id,$meta,$field)";
            setup.Parameters.AddWithValue("$id", fieldId.ToString());
            setup.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(field.MetaInfo, JsonSettings.Options));
            setup.Parameters.AddWithValue("$field", JsonSerializer.Serialize(field, JsonSettings.Options));
            setup.ExecuteNonQuery();
        }

        Assert.DoesNotThrow(() =>
            new SqlConnectionManager(connectionString, NullLogger<SqlConnectionManager>.Instance));

        using SqliteCommand read = keeper.CreateCommand();
        read.CommandText = "SELECT Field FROM FieldTable WHERE ID=$id";
        read.Parameters.AddWithValue("$id", fieldId.ToString());
        FieldModel migrated = JsonSerializer.Deserialize<FieldModel>((string)read.ExecuteScalar()!, JsonSettings.Options)!;
        Assert.Multiple(() =>
        {
            Assert.That(ExecuteScalar<long>(keeper, "PRAGMA user_version"), Is.EqualTo(SqlConnectionManager.CURRENT_SCHEMA_VERSION));
            Assert.That(migrated.Name, Is.EqualTo("Preserved"));
            Assert.That(migrated.CreationDate, Is.Not.Null);
            Assert.That(migrated.LastModificationDate, Is.EqualTo(migrated.CreationDate));
        });
    }

    private static string CreateSharedInMemoryConnectionString() =>
        $"Data Source=FieldSchema-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private static T ExecuteScalar<T>(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }
}
