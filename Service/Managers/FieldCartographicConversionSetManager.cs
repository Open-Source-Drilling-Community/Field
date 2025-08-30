using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Field.Model;
using NORCE.Drilling.Field.ModelShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NORCE.Drilling.Field.Service.Managers
{
    /// <summary>
    /// A manager for FieldCartographicConversionSet. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class FieldCartographicConversionSetManager
    {
        private static FieldCartographicConversionSetManager? _instance = null;
        private readonly ILogger<FieldCartographicConversionSetManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private FieldCartographicConversionSetManager(ILogger<FieldCartographicConversionSetManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldCartographicConversionSetManager GetInstance(ILogger<FieldCartographicConversionSetManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldCartographicConversionSetManager(logger, connectionManager);
            return _instance;
        }

        public int Count
        {
            get
            {
                int count = 0;
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM FieldCartographicConversionSetTable";
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
                        _logger.LogError(ex, "Impossible to count records in the FieldCartographicConversionSetTable");
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
                    //empty FieldCartographicConversionSetTable
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM FieldCartographicConversionSetTable";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    success = true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to clear the FieldCartographicConversionSetTable");
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
                command.CommandText = $"SELECT COUNT(*) FROM FieldCartographicConversionSetTable WHERE ID = '{guid}'";
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
                    _logger.LogError(ex, "Impossible to count rows from FieldCartographicConversionSetTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        /// <summary>
        /// Returns the list of Guid of all FieldCartographicConversionSet present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all FieldCartographicConversionSet present in the microservice database</returns>
        public List<Guid>? GetAllFieldCartographicConversionSetId()
        {
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID FROM FieldCartographicConversionSetTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        Guid id = reader.GetGuid(0);
                        ids.Add(id);
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from FieldCartographicConversionSetTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from FieldCartographicConversionSetTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all FieldCartographicConversionSet present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all FieldCartographicConversionSet present in the microservice database</returns>
        public List<ModelShared.MetaInfo?>? GetAllFieldCartographicConversionSetMetaInfo()
        {
            List<ModelShared.MetaInfo?> metaInfos = new();
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo FROM FieldCartographicConversionSetTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string mInfo = reader.GetString(0);
                        ModelShared.MetaInfo? metaInfo = JsonSerializer.Deserialize<ModelShared.MetaInfo>(mInfo, JsonSettings.Options);
                        metaInfos.Add(metaInfo);
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from FieldCartographicConversionSetTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from FieldCartographicConversionSetTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the FieldCartographicConversionSet identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the FieldCartographicConversionSet identified by its Guid from the microservice database</returns>
        public Model.FieldCartographicConversionSet? GetFieldCartographicConversionSetById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.FieldCartographicConversionSet? fieldCartographicConversionSet;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT FieldCartographicConversionSet FROM FieldCartographicConversionSetTable WHERE ID = '{guid}'";
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            fieldCartographicConversionSet = JsonSerializer.Deserialize<Model.FieldCartographicConversionSet>(data, JsonSettings.Options);
                            if (fieldCartographicConversionSet != null && fieldCartographicConversionSet.MetaInfo != null && !fieldCartographicConversionSet.MetaInfo.ID.Equals(guid))
                                throw new SqliteException("SQLite database corrupted: returned FieldCartographicConversionSet is null or has been jsonified with the wrong ID.", 1);
                        }
                        else
                        {
                            _logger.LogInformation("No FieldCartographicConversionSet of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the FieldCartographicConversionSet with the given ID from FieldCartographicConversionSetTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the FieldCartographicConversionSet of given ID from FieldCartographicConversionSetTable");
                    return fieldCartographicConversionSet;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given FieldCartographicConversionSet ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all FieldCartographicConversionSet present in the microservice database 
        /// </summary>
        /// <returns>the list of all FieldCartographicConversionSet present in the microservice database</returns>
        public List<Model.FieldCartographicConversionSet?>? GetAllFieldCartographicConversionSet()
        {
            List<Model.FieldCartographicConversionSet?> vals = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT FieldCartographicConversionSet FROM FieldCartographicConversionSetTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.FieldCartographicConversionSet? fieldCartographicConversionSet = JsonSerializer.Deserialize<Model.FieldCartographicConversionSet>(data, JsonSettings.Options);
                        vals.Add(fieldCartographicConversionSet);
                    }
                    _logger.LogInformation("Returning the list of existing FieldCartographicConversionSet from FieldCartographicConversionSetTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get FieldCartographicConversionSet from FieldCartographicConversionSetTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all FieldCartographicConversionSetLight present in the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the list of FieldCartographicConversionSetLight present in the microservice database</returns>
        public List<Model.FieldCartographicConversionSetLight>? GetAllFieldCartographicConversionSetLight()
        {
            List<Model.FieldCartographicConversionSetLight>? fieldCartographicConversionSetLightList = [];
            var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT MetaInfo, Name, Description, CreationDate, LastModificationDate, FieldName, FieldDescription FROM FieldCartographicConversionSetTable";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string metaInfoStr = reader.GetString(0);
                        OSDC.DotnetLibraries.General.DataManagement.MetaInfo? metaInfo = JsonSerializer.Deserialize<OSDC.DotnetLibraries.General.DataManagement.MetaInfo>(metaInfoStr, JsonSettings.Options);
                        string name = reader.GetString(1);
                        string descr = reader.GetString(2);
                        // make sure DateTimeOffset are properly instantiated when stored values are null (and parsed as empty string)
                        DateTimeOffset? creationDate = null;
                        if (DateTimeOffset.TryParse(reader.GetString(3), out DateTimeOffset cDate))
                            creationDate = cDate;
                        DateTimeOffset? lastModificationDate = null;
                        if (DateTimeOffset.TryParse(reader.GetString(4), out DateTimeOffset lDate))
                            lastModificationDate = lDate;
                        string fieldName = reader.GetString(5);
                        string fieldDescr = reader.GetString(6);
                        fieldCartographicConversionSetLightList.Add(new Model.FieldCartographicConversionSetLight(
                                metaInfo,
                                string.IsNullOrEmpty(name) ? null : name,
                                string.IsNullOrEmpty(descr) ? null : descr,
                                creationDate,
                                lastModificationDate,
                                fieldName,
                                fieldDescr));
                    }
                    _logger.LogInformation("Returning the list of existing FieldCartographicConversionSetLight from FieldCartographicConversionSetTable");
                    return fieldCartographicConversionSetLightList;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get light datas from FieldCartographicConversionSetTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Performs calculation on the given FieldCartographicConversionSet and adds it to the microservice database
        /// </summary>
        /// <param name="fieldCartographicConversionSet"></param>
        /// <returns>true if the given FieldCartographicConversionSet has been added successfully to the microservice database</returns>
        public async Task<bool> AddFieldCartographicConversionSet(Model.FieldCartographicConversionSet? fieldCartographicConversionSet)
        {
            if (fieldCartographicConversionSet != null && fieldCartographicConversionSet.MetaInfo != null && fieldCartographicConversionSet.MetaInfo.ID != Guid.Empty)
            {
                // calculate outputs: part of the computation is run through CartographicProjection µS, part through GeodeticDatum µS
                fieldCartographicConversionSet = await ManageFieldCartographicConversionSetAsync(fieldCartographicConversionSet);
                if (fieldCartographicConversionSet == null || fieldCartographicConversionSet.MetaInfo == null)
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given FieldCartographicConversionSet");
                    return false;
                }
                //if successful, check if another parent data with the same ID was calculated/added during the calculation time
                Model.FieldCartographicConversionSet? newFieldCartographicConversionSet = GetFieldCartographicConversionSetById(fieldCartographicConversionSet.MetaInfo.ID);
                if (newFieldCartographicConversionSet == null)
                {
                    Model.Field? field = null;
                    if (FieldManager.Instance != null && fieldCartographicConversionSet.FieldID != null)
                    {
                        field = FieldManager.Instance.GetFieldById(fieldCartographicConversionSet.FieldID.Value);
                    }
                    if (field == null)
                    {
                        _logger.LogWarning("Impossible to get the Field of given ID from FieldManager");
                        return false;
                    }
                    //update FieldCartographicConversionSetTable
                    var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        bool success = true;
                        try
                        {
                            //add the FieldCartographicConversionSet to the FieldCartographicConversionSetTable
                            string metaInfo = JsonSerializer.Serialize(fieldCartographicConversionSet.MetaInfo, JsonSettings.Options);
                            string? cDate = null;
                            if (fieldCartographicConversionSet.CreationDate != null)
                                cDate = ((DateTimeOffset)fieldCartographicConversionSet.CreationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                            fieldCartographicConversionSet.LastModificationDate = DateTimeOffset.UtcNow;
                            string? lDate = ((DateTimeOffset)fieldCartographicConversionSet.LastModificationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                            string data = JsonSerializer.Serialize(fieldCartographicConversionSet, JsonSettings.Options);
                            var command = connection.CreateCommand();
                            command.CommandText = "INSERT INTO FieldCartographicConversionSetTable (" +
                               "ID, " +
                               "MetaInfo, " +
                               "Name, " +
                               "Description, " +
                               "CreationDate, " +
                               "LastModificationDate, " +
                               "FieldName, " +
                               "FieldDescription, " +
                               "FieldCartographicConversionSet" +
                               ") VALUES (" +
                               $"'{fieldCartographicConversionSet!.MetaInfo!.ID}', " +
                               $"'{metaInfo}', " +
                               $"'{fieldCartographicConversionSet.Name}', " +
                               $"'{fieldCartographicConversionSet.Description}', " +
                               $"'{cDate}', " +
                               $"'{lDate}', " +
                               $"'{field.Name}', " +
                               $"'{field.Description}', " +
                               $"'{data}'" +
                               ")";
                            int count = command.ExecuteNonQuery();
                            if (count != 1)
                            {
                                _logger.LogWarning("Impossible to insert the given FieldCartographicConversionSet into the FieldCartographicConversionSetTable");
                                success = false;
                            }
                        }
                        catch (SqliteException ex)
                        {
                            _logger.LogError(ex, "Impossible to add the given FieldCartographicConversionSet into FieldCartographicConversionSetTable");
                            success = false;
                        }
                        //finalizing SQL transaction
                        if (success)
                        {
                            transaction.Commit();
                            _logger.LogInformation("Added the given FieldCartographicConversionSet of given ID into the FieldCartographicConversionSetTable successfully");
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
                    _logger.LogWarning("Impossible to post FieldCartographicConversionSet. ID already found in database.");
                    return false;
                }

            }
            else
            {
                _logger.LogWarning("The FieldCartographicConversionSet ID or the ID of its input are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given FieldCartographicConversionSet and updates it in the microservice database
        /// </summary>
        /// <param name="fieldCartographicConversionSet"></param>
        /// <returns>true if the given FieldCartographicConversionSet has been updated successfully</returns>
        public async Task<bool> UpdateFieldCartographicConversionSetById(Guid guid, Model.FieldCartographicConversionSet? fieldCartographicConversionSet)
        {
            bool success = true;
            if (guid != Guid.Empty && fieldCartographicConversionSet != null && fieldCartographicConversionSet.MetaInfo != null && fieldCartographicConversionSet.MetaInfo.ID == guid)
            {
                fieldCartographicConversionSet = await ManageFieldCartographicConversionSetAsync(fieldCartographicConversionSet);
                if (fieldCartographicConversionSet == null || fieldCartographicConversionSet.MetaInfo == null)
                {
                    _logger.LogWarning("Impossible to calculate outputs for the given FieldCartographicConversionSet");
                    return false;
                }
                Model.Field? field = null;
                if (FieldManager.Instance != null && fieldCartographicConversionSet.FieldID != null)
                {
                    field = FieldManager.Instance.GetFieldById(fieldCartographicConversionSet.FieldID.Value);
                }
                if (fieldCartographicConversionSet == null)
                {
                    _logger.LogWarning("Impossible to get the Field of given ID from FieldManager");
                    return false;
                }
                //update FieldCartographicConversionSetTable
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in FieldCartographicConversionSetTable
                    try
                    {
                        string metaInfo = JsonSerializer.Serialize(fieldCartographicConversionSet.MetaInfo, JsonSettings.Options);
                        string? cDate = null;
                        if (fieldCartographicConversionSet.CreationDate != null)
                            cDate = ((DateTimeOffset)fieldCartographicConversionSet.CreationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                        fieldCartographicConversionSet.LastModificationDate = DateTimeOffset.UtcNow;
                        string? lDate = ((DateTimeOffset)fieldCartographicConversionSet.LastModificationDate).ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                        string data = JsonSerializer.Serialize(fieldCartographicConversionSet, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.CommandText = $"UPDATE FieldCartographicConversionSetTable SET " +
                            $"MetaInfo = '{metaInfo}', " +
                            $"Name = '{fieldCartographicConversionSet.Name}', " +
                            $"Description = '{fieldCartographicConversionSet.Description}', " +
                            $"FieldCartographicConversionSet = '{data}', " +
                            $"CreationDate = '{cDate}', " +
                            $"LastModificationDate = '{lDate}', " +
                            $"FieldName = '{field!.Name}', " +
                            $"FieldDescription = '{field.Description}' " +
                            $"WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to update the FieldCartographicConversionSet");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to update the FieldCartographicConversionSet");
                        success = false;
                    }

                    // Finalizing
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Updated the given FieldCartographicConversionSet successfully");
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
                _logger.LogWarning("The FieldCartographicConversionSet ID or the ID of some of its attributes are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Deletes the FieldCartographicConversionSet of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the FieldCartographicConversionSet was deleted from the microservice database</returns>
        public bool DeleteFieldCartographicConversionSetById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    //delete FieldCartographicConversionSet from FieldCartographicConversionSetTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"DELETE FROM FieldCartographicConversionSetTable WHERE ID = '{guid}'";
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the FieldCartographicConversionSet of given ID from the FieldCartographicConversionSetTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the FieldCartographicConversionSet of given ID from FieldCartographicConversionSetTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the FieldCartographicConversionSet of given ID from the FieldCartographicConversionSetTable successfully");
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
                _logger.LogWarning("The FieldCartographicConversionSet ID is null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given FieldCartographicConversionSet by running through CartographicProjection µS (projections) and through GeodeticDatum µS (transformations)
        /// - cartographic conversion set is separated into 2 subsets depending on user's inputs (from cartographic coordinates, or from geodetic coordinates)
        /// - if cartographic coordinates (Northing, Easting, TVD) are provided
        ///     - then de-projection to the geodetic coordinates relative to the reference geodetic datum is done first
        ///     - then transformation from resulting geodetic coordinates to other geodetic coordinates (WGS84, octree code) are performed
        /// - if geodetic coordinates (datum, WGS84, octree code) are provided
        ///     - then transformation from whichever is set to the others is done first
        ///     - then projection to cartographic coordinates is performed
        /// Geodetic coordinates transformation call is implemented two ways:
        /// - best practice: call to GeodeticDatum µS (requires GeodeticDatum µS dependency)
        /// - best performance: direct call to GeodeticDatum.Model calculation method (requires GeodeticDatum.Model nuget install)
        /// </summary>
        /// <param name="fieldCartographicConversionSet"></param>
        /// <returns>true if the given FieldCartographicConversionSet has been updated successfully</returns>
        private async Task<Model.FieldCartographicConversionSet?> ManageFieldCartographicConversionSetAsync(Model.FieldCartographicConversionSet? fieldCartographicConversionSet)
        {
            if (fieldCartographicConversionSet != null && fieldCartographicConversionSet.FieldID != null &&
                fieldCartographicConversionSet.CartographicCoordinateList != null && fieldCartographicConversionSet.CartographicCoordinateList.Count != 0)
            {
                Model.Field? field = null;
                if (FieldManager.Instance != null)
                {
                    field = FieldManager.Instance.GetFieldById(fieldCartographicConversionSet.FieldID.Value);
                }
                if (field == null || field.CartographicProjectionID == null || field.CartographicProjectionID == Guid.Empty)
                {
                    _logger.LogWarning("Impossible to get the Field of given ID from Field microservice");
                    return null;
                }
                CartographicProjection cartographicProjection = await APIUtils.ClientCartographicProjection.GetCartographicProjectionByIdAsync(field.CartographicProjectionID.Value);
                if (cartographicProjection == null)
                {
                    _logger.LogWarning("Impossible to get the CartographicProjection of given ID from CartographicProjection microservice");
                    return null;
                }
                CartographicConversionSet cartographicConversionSet = new()
                {
                    MetaInfo = new ModelShared.MetaInfo() { ID = Guid.NewGuid() },
                    Name = fieldCartographicConversionSet.Name,
                    Description = fieldCartographicConversionSet.Description,
                    CreationDate = fieldCartographicConversionSet.CreationDate,
                    LastModificationDate = fieldCartographicConversionSet.LastModificationDate,
                    CartographicProjectionID = field.CartographicProjectionID,
                    CartographicCoordinateList = fieldCartographicConversionSet.CartographicCoordinateList
                };
                try
                {
                    await APIUtils.ClientCartographicProjection.PostCartographicConversionSetAsync(cartographicConversionSet);
                    CartographicConversionSet calculatedCartographicConversionSet = await APIUtils.ClientCartographicProjection.GetCartographicConversionSetByIdAsync(cartographicConversionSet.MetaInfo.ID);
                    await APIUtils.ClientCartographicProjection.DeleteCartographicConversionSetByIdAsync(cartographicConversionSet.MetaInfo.ID);
                    if (calculatedCartographicConversionSet == null || calculatedCartographicConversionSet.CartographicCoordinateList == null ||
                        calculatedCartographicConversionSet.CartographicCoordinateList.Count != fieldCartographicConversionSet.CartographicCoordinateList.Count)
                    {
                        _logger.LogWarning("Impossible to get the computed CartographicConversionSet from CartographicProjection microservice");
                        return null;
                    }
                    if (fieldCartographicConversionSet.CartographicCoordinateList == null)
                        fieldCartographicConversionSet.CartographicCoordinateList = [];
                    else
                        fieldCartographicConversionSet.CartographicCoordinateList.Clear();
                    foreach (var coord in calculatedCartographicConversionSet.CartographicCoordinateList)
                    {
                        fieldCartographicConversionSet.CartographicCoordinateList.Add(coord);
                    }
                } catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to post the given CartographicConversionSet to CartographicProjection microservice");
                    return null;
                }
            }
            return fieldCartographicConversionSet;
        }
    }
}
