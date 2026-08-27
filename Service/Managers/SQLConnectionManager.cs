using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace NORCE.Drilling.Field.Service.Managers
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
        private readonly IReadOnlyDictionary<Guid, Guid> _projectionMappings;
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
        public static readonly string DATABASE_FILENAME = "Field.db";
        public static readonly string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public const int CURRENT_SCHEMA_VERSION = 2;

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
            ILogger<SqlConnectionManager> logger,
            IReadOnlyDictionary<Guid, Guid>? projectionMappings = null)
        {
            _connectionString = connectionString;
            _logger = logger;
            _projectionMappings = projectionMappings ?? new Dictionary<Guid, Guid>();
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
            var connection = GetConnection();
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

                bool hasObsoleteCalculationTable = tableNameList.Contains("FieldCartographicConversionSetTable", StringComparer.Ordinal);
                FieldProjectionMigrationPlan projectionPlan = tableNameList.Contains("FieldTable", StringComparer.Ordinal)
                    ? FieldProjectionReferenceMigrator.Plan(connection, _projectionMappings)
                    : new FieldProjectionMigrationPlan([]);

                // Back up the complete SQLite database before the first persisted migration.
                // The backup remains alongside Field.db and is independent of the logical
                // off-machine JSON backup made before deployment.
                if (hasObsoleteCalculationTable || projectionPlan.HasChanges)
                {
                    BackupBeforeMigration(connection);
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        FieldProjectionReferenceMigrator.Apply(connection, transaction, projectionPlan);
                        if (hasObsoleteCalculationTable)
                        {
                            using SqliteCommand drop = connection.CreateCommand();
                            drop.Transaction = transaction;
                            drop.CommandText = "DROP TABLE FieldCartographicConversionSetTable";
                            drop.ExecuteNonQuery();
                        }
                        using SqliteCommand version = connection.CreateCommand();
                        version.Transaction = transaction;
                        version.CommandText = $"PRAGMA user_version = {CURRENT_SCHEMA_VERSION}";
                        version.ExecuteNonQuery();
                        transaction.Commit();
                        tableNameList.Remove("FieldCartographicConversionSetTable");
                        _logger.LogInformation("Migrated Field database to schema version {Version}; {FieldCount} field projection references changed and obsolete calculation cases removed={RemovedCases}", CURRENT_SCHEMA_VERSION, projectionPlan.Changes.Count, hasObsoleteCalculationTable);
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
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
                using SqliteCommand schemaVersion = connection.CreateCommand();
                schemaVersion.CommandText = $"PRAGMA user_version = {CURRENT_SCHEMA_VERSION}";
                schemaVersion.ExecuteNonQuery();
            }
            else
            {
                _logger.LogError("Problem opening a new connection while managing database");
            }
        }

        private void BackupBeforeMigration(SqliteConnection source)
        {
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            string sourcePath = Path.GetFullPath(builder.DataSource);
            string directory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException("The Field database path has no parent directory.");
            string backupPath = Path.Combine(directory, $"Field.pre-v{CURRENT_SCHEMA_VERSION}.{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}.db");
            using var destination = new SqliteConnection($"Data Source={backupPath}");
            destination.Open();
            source.BackupDatabase(destination);
            _logger.LogWarning("Created pre-migration Field database backup at {BackupPath}", backupPath);
        }

        /// <summary>
        /// Check that expected fields (in tableStructure.Value) exactly match those of the stored database
        /// </summary>
        /// <param name="tableStructure"></param>
        /// <returns>true if the expected fields exactly match fields of the stored database</returns>
        private bool CheckDatabaseStructure(KeyValuePair<string, string[]> tableStructure)
        {
            var connection = GetConnection();
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
            var connection = GetConnection();
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
            var connection = GetConnection();
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
            var connection = GetConnection();
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
