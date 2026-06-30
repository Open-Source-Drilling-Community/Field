using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NORCE.Drilling.Field.Service.Managers
{
    public class FieldDelineationLineTypeManager
    {
        private static FieldDelineationLineTypeManager? _instance;
        private readonly ILogger<FieldDelineationLineTypeManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly string[] DefaultNames =
        [
            "country border line",
            "lease line",
            "field delineation",
            "block",
            "protected area",
            "military zone",
            "cable corridor",
            "pipeline corridor",
            "navigation corridor",
            "environmental exclusion zone",
            "safety zone",
            "restricted area",
            "no drilling zone"
        ];

        private FieldDelineationLineTypeManager(ILogger<FieldDelineationLineTypeManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldDelineationLineTypeManager GetInstance(ILogger<FieldDelineationLineTypeManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldDelineationLineTypeManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllFieldDelineationLineTypeId()
        {
            EnsureDefaultLineTypes();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM FieldDelineationLineTypeTable";
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
                _logger.LogError(ex, "Impossible to get IDs from FieldDelineationLineTypeTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllFieldDelineationLineTypeMetaInfo()
        {
            EnsureDefaultLineTypes();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM FieldDelineationLineTypeTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from FieldDelineationLineTypeTable");
                return null;
            }
        }

        public Model.FieldDelineationLineType? GetFieldDelineationLineTypeById(Guid guid)
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
            command.CommandText = $"SELECT FieldDelineationLineType FROM FieldDelineationLineTypeTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.FieldDelineationLineType? data = JsonSerializer.Deserialize<Model.FieldDelineationLineType>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned FieldDelineationLineType has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldDelineationLineType from FieldDelineationLineTypeTable");
            }

            return null;
        }

        public List<Model.FieldDelineationLineType?>? GetAllFieldDelineationLineType()
        {
            EnsureDefaultLineTypes();
            List<Model.FieldDelineationLineType?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FieldDelineationLineType FROM FieldDelineationLineTypeTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.FieldDelineationLineType>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldDelineationLineType from FieldDelineationLineTypeTable");
                return null;
            }
        }

        public bool AddFieldDelineationLineType(Model.FieldDelineationLineType? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetFieldDelineationLineTypeById(data.MetaInfo.ID) != null)
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
                command.CommandText = "INSERT INTO FieldDelineationLineTypeTable (" +
                    "ID, MetaInfo, Name, CreationDate, LastModificationDate, FieldDelineationLineType" +
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
                _logger.LogError(ex, "Impossible to add FieldDelineationLineType");
                return false;
            }
        }

        public bool UpdateFieldDelineationLineTypeById(Guid guid, Model.FieldDelineationLineType? data)
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
                command.CommandText = $"UPDATE FieldDelineationLineTypeTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"FieldDelineationLineType = '{serialized}' " +
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
                _logger.LogError(ex, "Impossible to update FieldDelineationLineType");
                return false;
            }
        }

        public bool DeleteFieldDelineationLineTypeById(Guid guid)
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
                command.CommandText = $"DELETE FROM FieldDelineationLineTypeTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete FieldDelineationLineType");
                return false;
            }
        }

        private void EnsureDefaultLineTypes()
        {
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM FieldDelineationLineTypeTable";
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
                _logger.LogError(ex, "Impossible to count FieldDelineationLineTypeTable");
                return;
            }

            foreach (string name in DefaultNames)
            {
                AddFieldDelineationLineType(new Model.FieldDelineationLineType
                {
                    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                    Name = name
                });
            }
        }
    }
}
