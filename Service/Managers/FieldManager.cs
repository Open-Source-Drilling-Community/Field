using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace NORCE.Drilling.Field.Service.Managers
{
    /// <summary>
    /// A manager for Field. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class FieldManager
    {
        private static FieldManager? _instance = null;
        private readonly ILogger<FieldManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private FieldManager(ILogger<FieldManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldManager GetInstance(ILogger<FieldManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldManager(logger, connectionManager);
            return _instance;
        }
        internal static FieldManager Instance { get { return _instance; } }

        public int Count
        {
            get
            {
                int count = 0;
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM FieldTable";
                    try
                    {
                        using SqliteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to count records in the FieldTable");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
                return count;
            }
        }

        public bool Clear()
        {
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                bool success = false;
                using var transaction = connection.BeginTransaction();
                try
                {
                    //empty FieldTable
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM FieldTable";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    success = true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to clear the FieldTable");
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(Guid guid)
        {
            int count = 0;
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM FieldTable WHERE ID = '{guid}'";
                try
                {
                    using SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        count = (int)reader.GetInt64(0);
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to count rows from FieldTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        /// <summary>
        /// Returns the list of Guid of all Field present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all Field present in the microservice database</returns>
        public List<Guid>? GetAllFieldId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM FieldTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Guid id = reader.GetGuid(0);
                        ids.Add(id);
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from FieldTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from FieldTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Field present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all Field present in the microservice database</returns>
        public List<MetaInfo?>? GetAllFieldMetaInfo()
        {
            List<MetaInfo?> metaInfos = new();
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM FieldTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string mInfo = reader.GetString(0);
                        MetaInfo? metaInfo = JsonSerializer.Deserialize<MetaInfo>(mInfo, JsonSettings.Options);
                        metaInfos.Add(metaInfo);
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from FieldTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from FieldTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the Field identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Field identified by its Guid from the microservice database</returns>
        public Model.Field? GetFieldById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Field? field;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT Field FROM FieldTable WHERE ID = '{guid}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            field = JsonSerializer.Deserialize<Model.Field>(data, JsonSettings.Options);
                            if (field != null && field.MetaInfo != null && !field.MetaInfo.ID.Equals(guid))
                                throw new SqliteException("SQLite database corrupted: returned Field is null or has been jsonified with the wrong ID.", 1);
                        }
                        else
                        {
                            _logger.LogInformation("No Field of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the Field with the given ID from FieldTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Field of given ID from FieldTable");
                    return field;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given Field ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all Field present in the microservice database 
        /// </summary>
        /// <returns>the list of all Field present in the microservice database</returns>
        public List<Model.Field?>? GetAllField()
        {
            List<Model.Field?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Field FROM FieldTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.Field? field = JsonSerializer.Deserialize<Model.Field>(data, JsonSettings.Options);
                        vals.Add(field);
                    }
                    _logger.LogInformation("Returning the list of existing Field from FieldTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get Field from FieldTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }


        /// <summary>
        /// Performs calculation on the given Field and adds it to the microservice database
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if the given Field has been added successfully to the microservice database</returns>
        public bool AddField(Model.Field? field)
        {
            if (field != null && field.MetaInfo != null && field.MetaInfo.ID != Guid.Empty)
            {
                //if successful, check if another parent data with the same ID was calculated/added during the calculation time
                Model.Field? newField = GetFieldById(field.MetaInfo.ID);
                if (newField == null)
                {
                    //update FieldTable
                    var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        bool success = true;
                        try
                        {
                            //add the Field to the FieldTable
                            string metaInfo = JsonSerializer.Serialize(field.MetaInfo, JsonSettings.Options);
                            string data = JsonSerializer.Serialize(field, JsonSettings.Options);
                            var command = connection.CreateCommand();
                            command.CommandText = "INSERT INTO FieldTable (" +
                                "ID, " +
                                "MetaInfo, " +
                                "Field" +
                                ") VALUES (" +
                                $"'{field.MetaInfo.ID}', " +
                                $"'{metaInfo}', " +
                                $"'{data}'" +
                                ")";
                            int count = command.ExecuteNonQuery();
                            if (count != 1)
                            {
                                _logger.LogWarning("Impossible to insert the given Field into the FieldTable");
                                success = false;
                            }
                        }
                        catch (SqliteException ex)
                        {
                            _logger.LogError(ex, "Impossible to add the given Field into FieldTable");
                            success = false;
                        }
                        //finalizing SQL transaction
                        if (success)
                        {
                            transaction.Commit();
                            _logger.LogInformation("Added the given Field of given ID into the FieldTable successfully");
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                        return success;
                    }
                    else
                    {
                        _logger.LogWarning("Impossible to access the SQLite database");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to post Field. ID already found in database.");
                    return false;
                }

            }
            else
            {
                _logger.LogWarning("The Field ID or the ID of its input are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given Field and updates it in the microservice database
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if the given Field has been updated successfully</returns>
        public bool UpdateFieldById(Guid guid, Model.Field? field)
        {
            bool success = true;
            if (guid != Guid.Empty && field != null && field.MetaInfo != null && field.MetaInfo.ID == guid)
            {
                //update FieldTable
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in FieldTable
                    try
                    {
                        string metaInfo = JsonSerializer.Serialize(field.MetaInfo, JsonSettings.Options);
                        string data = JsonSerializer.Serialize(field, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.CommandText = $"UPDATE FieldTable SET " +
                            $"MetaInfo = '{metaInfo}', " +
                            $"Field = '{data}' " +
                            $"WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to update the Field");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to update the Field");
                        success = false;
                    }

                    // Finalizing
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Updated the given Field successfully");
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Field ID or the ID of some of its attributes are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Deletes the Field of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Field was deleted from the microservice database</returns>
        public bool DeleteFieldById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    //delete Field from FieldTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"DELETE FROM FieldTable WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the Field of given ID from the FieldTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the Field of given ID from FieldTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the Field of given ID from the FieldTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Field ID is null or empty");
            }
            return false;
        }
    }
}