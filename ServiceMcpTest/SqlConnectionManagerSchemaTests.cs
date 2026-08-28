using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OSDC.Drilling.Field.Service.Managers;

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
    public void Empty_database_is_created_at_v2_and_can_be_reopened()
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

    private static string CreateSharedInMemoryConnectionString() =>
        $"Data Source=FieldSchema-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private static T ExecuteScalar<T>(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }
}
