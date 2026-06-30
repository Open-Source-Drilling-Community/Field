using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NORCE.Drilling.Field.Service.Managers
{
    public class FieldIdentityManager
    {
        private static FieldIdentityManager? _instance;
        private readonly ILogger<FieldIdentityManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly string[] DefaultIdentities =
        [
            "Official name",
            "Short name",
            "Common name",
            "Historical name",
            "Alternate name",
            "Sokkeldirektorat ID",
            "License number",
            "API number",
            "Daily Drilling Report ID",
            "AFE number",
            "WITSML UID",
            "Project number",
            "External database ID"
        ];

        private FieldIdentityManager(ILogger<FieldIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldIdentityManager GetInstance(ILogger<FieldIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldIdentityManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllFieldIdentityId()
        {
            EnsureDefaultIdentities();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM FieldIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    ids.Add(reader.GetGuid(0));
                }
                return ids;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get IDs from FieldIdentityTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllFieldIdentityMetaInfo()
        {
            EnsureDefaultIdentities();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM FieldIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    metaInfos.Add(JsonSerializer.Deserialize<MetaInfo>(reader.GetString(0), JsonSettings.Options));
                }
                return metaInfos;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get MetaInfo from FieldIdentityTable");
                return null;
            }
        }

        public Model.FieldIdentity? GetFieldIdentityById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT FieldIdentity FROM FieldIdentityTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.FieldIdentity? data = JsonSerializer.Deserialize<Model.FieldIdentity>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned FieldIdentity has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldIdentity from FieldIdentityTable");
            }

            return null;
        }

        public List<Model.FieldIdentity?>? GetAllFieldIdentity()
        {
            EnsureDefaultIdentities();
            List<Model.FieldIdentity?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FieldIdentity FROM FieldIdentityTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.FieldIdentity>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldIdentity from FieldIdentityTable");
                return null;
            }
        }

        public bool AddFieldIdentity(Model.FieldIdentity? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetFieldIdentityById(data.MetaInfo.ID) != null)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                data.CreationDate ??= now;
                data.LastModificationDate = now;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO FieldIdentityTable (" +
                    "ID, MetaInfo, Name, CreationDate, LastModificationDate, FieldIdentity" +
                    ") VALUES (" +
                    $"'{data.MetaInfo.ID}', '{metaInfo}', '{data.Name}', '{creationDate}', '{lastModificationDate}', '{serialized}')";
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to add FieldIdentity");
                return false;
            }
        }

        public bool UpdateFieldIdentityById(Guid guid, Model.FieldIdentity? data)
        {
            if (guid == Guid.Empty || data?.MetaInfo == null || data.MetaInfo.ID != guid)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                data.LastModificationDate = DateTimeOffset.UtcNow;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = $"UPDATE FieldIdentityTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"FieldIdentity = '{serialized}' " +
                    $"WHERE ID = '{guid}'";
                int count = command.ExecuteNonQuery();
                if (count != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to update FieldIdentity");
                return false;
            }
        }

        public bool DeleteFieldIdentityById(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return false;
            }

            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM FieldIdentityTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete FieldIdentity");
                return false;
            }
        }

        private void EnsureDefaultIdentities()
        {
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM FieldIdentityTable";
            try
            {
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read() && reader.GetInt64(0) > 0)
                {
                    return;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to count FieldIdentityTable");
                return;
            }

            foreach (string name in DefaultIdentities)
            {
                AddFieldIdentity(new Model.FieldIdentity
                {
                    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                    Name = name
                });
            }
        }
    }
}
