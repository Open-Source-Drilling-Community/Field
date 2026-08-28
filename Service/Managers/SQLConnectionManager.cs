using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.Service.Managers
{
    /// <summary>
    /// A manager for the sql database connection, registered as a singleton through dependency injection (see Program.cs)
    /// Prior to creating a database, existing database structure is checked for consistency with the structure defined in tableStructureDict_
    /// If inconsistent (table count, table names, fields count, fields names), a timestamped backup of the existing database is generated first
    /// </summary>
    /// <remarks>
    /// SQLite database connection strategy:
    /// - single connection for every access (chosen strategy in the general case)
    ///     each access to the database is performed through isolated connections stored in a List of connections
    ///     > isolation, reliability, fail-safe, thread-safe, but overhead due to opening connections
    /// - shared connection between access
    ///     one connection is opened for the lifetime of the application and used to access database through various web requests and commands 
    ///     > no overhead, but issues with concurrency, single-point of failure, state management
    /// - scoped connection (registering service with AddScoped rather than AddSingleton)
    ///     one connection is opened per web request
    ///     > same problems as with shared connection, but limited to the scope of one webrequest rather than to the whole lifetime of the application
    /// </remarks>
    public class SqlConnectionManager
    {
        private readonly ILogger<SqlConnectionManager> _logger;
        private readonly string _connectionString;
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
        public static readonly string DATABASE_FILENAME = "Field.db";
        public static readonly string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public const int CURRENT_SCHEMA_VERSION = 3;

        // dictionary describing tables format
        private readonly static Dictionary<string, string[]> _tableStructureDict = new Dictionary<string, string[]>()
            {
                { "FieldTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Field text" }
                },
                { "FieldDelineationLineTypeTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldDelineationLineType text" }
                },
                { "FieldFeatureCategoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "IsExclusive integer",
                    "HasValidityPeriod integer",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldFeatureCategory text" }
                },
                { "FieldIdentityTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldIdentity text" }
                },
                { "FieldMembershipCategoryTable", new string[] {
                    "ID text primary key",
                    "MetaInfo text",
                    "Name text",
                    "IsExclusive integer",
                    "HasValidityPeriod integer",
                    "CreationDate text",
                    "LastModificationDate text",
                    "FieldMembershipCategory text" }
                }
            };

        public SqlConnectionManager(
            string connectionString,
            ILogger<SqlConnectionManager> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            _logger.LogInformation("SqliteConnectionManager created");
            if (Initialize())
            {
                ManageDataBase();
            }
            else
            {
                throw new InvalidOperationException("Unable to initialize the Field database storage directory.");
            }
        }

        public SqliteConnection? GetConnection()
        {
            // a new SQL connection is opened for every transaction, thus ensuring thread-safety and removing unnecessary locks
            var connection = new SqliteConnection(_connectionString);
            if (connection != null)
            {
                connection.Open();
            }
            else
            {
                _logger.LogError("Problem while opening SQLite connection");
            }
            return connection;
        }

        private bool Initialize()
        {
            if (!Directory.Exists(HOME_DIRECTORY))
            {
                _logger.LogInformation("Creating home directory");
                try
                {
                    Directory.CreateDirectory(HOME_DIRECTORY);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create home directory for local storage");
                    return false;
                }
            }
            if (Directory.Exists(HOME_DIRECTORY))
            {
                try
                {
                    string databaseFileName = HOME_DIRECTORY + Path.DirectorySeparatorChar + DATABASE_FILENAME;
                    if (File.Exists(databaseFileName))
                    {
                        _logger.LogInformation("Opening database {_databaseFileName}", DATABASE_FILENAME);
                    }
                    else
                    {
                        _logger.LogInformation("Creating database {_databaseFileName}", DATABASE_FILENAME);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create {_databaseFileName}", DATABASE_FILENAME);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Home directory for local storage should have been created, check for access");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Applies explicit non-destructive schema migrations and verifies the resulting structure.
        /// Unexpected structures fail startup; they are never repaired by dropping user tables.
        /// </summary>
        private void ManageDataBase()
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                List<string> tableNameList = new();
                string query = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";

                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tableNameList.Add(reader.GetString(0));
                        }
                    }
                }

                int schemaVersion;
                using (SqliteCommand version = connection.CreateCommand())
                {
                    version.CommandText = "PRAGMA user_version";
                    schemaVersion = Convert.ToInt32(version.ExecuteScalar());
                }
                if (schemaVersion > CURRENT_SCHEMA_VERSION)
                {
                    throw new InvalidOperationException(
                        $"Field database schema version {schemaVersion} is newer than the supported version {CURRENT_SCHEMA_VERSION}.");
                }

                if (tableNameList.Count == 0)
                {
                    _logger.LogInformation("Creating Field database schema version {Version}", CURRENT_SCHEMA_VERSION);
                    foreach (var tableStruct in _tableStructureDict)
                    {
                        if (!CreateTable(tableStruct) || !IndexTable(tableStruct.Key))
                            throw new InvalidOperationException($"Unable to create required Field database table '{tableStruct.Key}'.");
                    }
                    using SqliteCommand version = connection.CreateCommand();
                    version.CommandText = $"PRAGMA user_version = {CURRENT_SCHEMA_VERSION}";
                    version.ExecuteNonQuery();
                    return;
                }

                if (schemaVersion == 2)
                {
                    MigrateVersion2ToVersion3(connection);
                    schemaVersion = CURRENT_SCHEMA_VERSION;
                }

                if (schemaVersion < CURRENT_SCHEMA_VERSION)
                {
                    throw new InvalidOperationException(
                        $"Field database schema version {schemaVersion} is no longer supported. " +
                        $"Restore it through the schema-version-2 batch API before starting this service.");
                }

                List<string> unexpected = tableNameList.Except(_tableStructureDict.Keys, StringComparer.Ordinal).ToList();
                List<string> missing = _tableStructureDict.Keys.Except(tableNameList, StringComparer.Ordinal).ToList();
                List<string> malformed = _tableStructureDict
                    .Where(table => tableNameList.Contains(table.Key, StringComparer.Ordinal) && !CheckDatabaseStructure(table))
                    .Select(table => table.Key)
                    .ToList();
                if (unexpected.Count > 0 || missing.Count > 0 || malformed.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Unexpected Field database structure. No data was changed. Unexpected=[{string.Join(',', unexpected)}], missing=[{string.Join(',', missing)}], malformed=[{string.Join(',', malformed)}].");
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while managing database");
            }
        }

        private void MigrateVersion2ToVersion3(SqliteConnection connection)
        {
            _logger.LogInformation("Migrating Field database schema from version 2 to version 3 by assigning server-owned timestamps where absent");
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                NormalizeFieldTimestamps(connection, transaction, now);
                NormalizeCatalogTimestamps<FieldFeatureCategory>(connection, transaction, now, "FieldFeatureCategoryTable", "FieldFeatureCategory",
                    value => value.CreationDate, (value, timestamp) => value.CreationDate = timestamp,
                    value => value.LastModificationDate, (value, timestamp) => value.LastModificationDate = timestamp);
                NormalizeCatalogTimestamps<FieldMembershipCategory>(connection, transaction, now, "FieldMembershipCategoryTable", "FieldMembershipCategory",
                    value => value.CreationDate, (value, timestamp) => value.CreationDate = timestamp,
                    value => value.LastModificationDate, (value, timestamp) => value.LastModificationDate = timestamp);
                NormalizeCatalogTimestamps<FieldIdentity>(connection, transaction, now, "FieldIdentityTable", "FieldIdentity",
                    value => value.CreationDate, (value, timestamp) => value.CreationDate = timestamp,
                    value => value.LastModificationDate, (value, timestamp) => value.LastModificationDate = timestamp);
                NormalizeCatalogTimestamps<FieldDelineationLineType>(connection, transaction, now, "FieldDelineationLineTypeTable", "FieldDelineationLineType",
                    value => value.CreationDate, (value, timestamp) => value.CreationDate = timestamp,
                    value => value.LastModificationDate, (value, timestamp) => value.LastModificationDate = timestamp);

                using SqliteCommand version = connection.CreateCommand();
                version.Transaction = transaction;
                version.CommandText = $"PRAGMA user_version = {CURRENT_SCHEMA_VERSION}";
                version.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void NormalizeFieldTimestamps(SqliteConnection connection, SqliteTransaction transaction, DateTimeOffset now)
        {
            List<(Guid ID, Model.Field Value)> values = [];
            using (SqliteCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = "SELECT ID, Field FROM FieldTable";
                using SqliteDataReader reader = read.ExecuteReader();
                while (reader.Read())
                {
                    Model.Field? value = JsonSerializer.Deserialize<Model.Field>(reader.GetString(1), JsonSettings.Options);
                    if (value != null) values.Add((reader.GetGuid(0), value));
                }
            }
            foreach ((Guid id, Model.Field value) in values)
            {
                if (value.CreationDate != null && value.LastModificationDate != null) continue;
                value.CreationDate ??= now;
                value.LastModificationDate ??= value.CreationDate;
                using SqliteCommand update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE FieldTable SET Field=$document WHERE ID=$id";
                update.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
                update.Parameters.AddWithValue("$id", id.ToString());
                update.ExecuteNonQuery();
            }
        }

        private static void NormalizeCatalogTimestamps<T>(SqliteConnection connection, SqliteTransaction transaction, DateTimeOffset now,
            string table, string documentColumn, Func<T, DateTimeOffset?> getCreated, Action<T, DateTimeOffset?> setCreated,
            Func<T, DateTimeOffset?> getModified, Action<T, DateTimeOffset?> setModified)
            where T : class
        {
            List<(Guid ID, T Value)> values = [];
            using (SqliteCommand read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = $"SELECT ID, {documentColumn} FROM {table}";
                using SqliteDataReader reader = read.ExecuteReader();
                while (reader.Read())
                {
                    T? value = JsonSerializer.Deserialize<T>(reader.GetString(1), JsonSettings.Options);
                    if (value != null) values.Add((reader.GetGuid(0), value));
                }
            }
            foreach ((Guid id, T value) in values)
            {
                if (getCreated(value) != null && getModified(value) != null) continue;
                setCreated(value, getCreated(value) ?? now);
                setModified(value, getModified(value) ?? getCreated(value));
                using SqliteCommand update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = $"UPDATE {table} SET CreationDate=$created, LastModificationDate=$modified, {documentColumn}=$document WHERE ID=$id";
                update.Parameters.AddWithValue("$created", getCreated(value)?.ToString(DATE_TIME_FORMAT) ?? (object)DBNull.Value);
                update.Parameters.AddWithValue("$modified", getModified(value)?.ToString(DATE_TIME_FORMAT) ?? (object)DBNull.Value);
                update.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
                update.Parameters.AddWithValue("$id", id.ToString());
                update.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Check that expected fields (in tableStructure.Value) exactly match those of the stored database
        /// </summary>
        /// <param name="tableStructure"></param>
        /// <returns>true if the expected fields exactly match fields of the stored database</returns>
        private bool CheckDatabaseStructure(KeyValuePair<string, string[]> tableStructure)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tableStructure.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"SELECT * FROM {key}");
                command.CommandText = sb.ToString();
                try
                {
                    using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        var schema = reader.GetSchemaTable();
                        if (tableStructure.Value.Length != schema.Rows.Count)
                            return false; // unexpected number of fields in table
                        foreach (string field in tableStructure.Value)
                        {
                            bool tmpSuccess = false;
                            foreach (DataRow col in schema.Rows)
                            {
                                if (field.Split(" ").ElementAt(0) == col.Field<string>("ColumnName"))
                                {
                                    tmpSuccess = true;
                                    break;
                                }
                            }
                            if (!tmpSuccess)
                                return false; // at least one expected field is not found in stored database
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to retrieve schema from table {key}", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while checking database structure");
                return false;
            }
            return true;
        }

        private bool CreateTable(KeyValuePair<string, string[]> tabStruct)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tabStruct.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {key} ()");
                foreach (string col in tabStruct.Value)
                {
                    sb.Insert(sb.Length - 1, col + ",");
                };
                sb.Remove(sb.Length - 2, 1);
                command.CommandText = sb.ToString();

                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{key} has been successfully created", key);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to create {key} which will be dropped", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

        private bool IndexTable(string dbName)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"CREATE UNIQUE INDEX {dbName}Index ON {dbName} (ID)";
                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{dbName} has been successfully indexed", dbName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to index {dbName} which will be dropped", dbName);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

        private bool DropTable(string dbName)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                            $"DROP TABLE {dbName}";
                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogWarning("{dbName} has been successfully dropped", dbName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to drop {dbName}", dbName);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }
    }
}
