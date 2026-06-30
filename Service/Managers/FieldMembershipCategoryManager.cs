using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NORCE.Drilling.Field.Service.Managers
{
    public class FieldMembershipCategoryManager
    {
        private static FieldMembershipCategoryManager? _instance;
        private readonly ILogger<FieldMembershipCategoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly DefaultFieldMembershipCategory[] DefaultCategories =
        [
            new("Basin", false, false, ["North Sea Basin", "Norwegian Sea Basin", "Barents Sea Basin", "Central Graben", "Viking Graben", "East Shetland Basin", "Møre Basin", "Vøring Basin", "Hammerfest Basin", "Rockall Basin"]),
            new("Play", false, false, ["Jurassic structural play", "Jurassic stratigraphic play", "Triassic clastic play", "Cretaceous chalk play", "Paleocene turbidite play", "Permian carbonate play", "Zechstein carbonate / evaporite play", "Basement fractured play", "Sub-basalt play"]),
            new("Stratigraphic unit", false, false, ["Brent Group", "Statfjord Formation", "Cook Formation", "Draupne Formation", "Hugin Formation", "Ula Formation", "Ekofisk Formation", "Tor Formation", "Lista Formation", "Heimdal Formation"]),
            new("Structural element", false, false, ["Viking Graben", "Central Graben", "Tampen Spur", "Utsira High", "Horda Platform", "Stord Basin", "Loppa High", "Hammerfest Basin", "Nordland Ridge", "Halibut Horst"]),
            new("Country", false, false, ["Norway", "United Kingdom", "Denmark", "Netherlands", "Germany", "Ireland", "Faroe Islands", "Iceland", "United States", "Canada"]),
            new("Block (Norway)", false, true, ["30/9", "31/2", "31/3", "34/7", "34/10", "35/8", "6406/3", "6507/11", "7120/8", "7220/8"]),
            new("Block (UK)", false, true, ["9/13", "15/25", "16/28", "21/30", "22/26", "29/3", "30/6", "44/22", "48/30", "49/26"]),
            new("Quadrant (Norway)", false, false, ["2", "7", "15", "16", "25", "30", "31", "34", "35", "6406", "6507", "7120", "7220"]),
            new("License", false, true, ["Production licence", "Exploration licence", "Retention licence", "Unit agreement", "Joint operating agreement", "Concession", "Mining lease", "Geothermal lease"]),
            new("Authority", false, true, ["Sokkeldirektoratet", "Havindustritilsynet", "Norwegian Ministry of Energy", "North Sea Transition Authority", "Danish Energy Agency", "Netherlands Enterprise Agency", "Bureau of Ocean Energy Management", "Bureau of Safety and Environmental Enforcement"]),
            new("Region", false, false, ["Northern North Sea", "Central North Sea", "Southern North Sea", "Norwegian Sea", "Barents Sea", "West of Shetland", "Gulf of Mexico", "Alberta Basin", "Permian Basin", "Middle East"]),
            new("Hub", false, true, ["Tampen hub", "Sleipner hub", "Oseberg hub", "Troll hub", "Heidrun hub", "Åsgard hub", "Ekofisk hub", "Forties hub", "Shetland gas plant", "Kårstø gas processing plant"]),
            new("Pipeline network", false, true, ["Statpipe", "Zeepipe", "Europipe", "Langeled", "Norpipe", "Franpipe", "FLAGS", "SAGE", "Forties Pipeline System", "CATS"]),
            new("Unitized field", false, true, ["Unitized field", "Cross-border unit", "Redetermination area", "Shared accumulation", "Unit operating agreement", "Non-unitized accumulation"]),
            new("Operator", true, true, ["Equinor", "Aker BP", "Vår Energi", "ConocoPhillips", "Shell", "TotalEnergies", "BP", "Harbour Energy", "Chevron", "ExxonMobil", "Petrobras", "Eni"]),
            new("Partners", false, true, ["Equinor", "Aker BP", "Vår Energi", "ConocoPhillips", "Shell", "TotalEnergies", "BP", "Harbour Energy", "Chevron", "ExxonMobil", "Petrobras", "Eni"]),
            new("Business unit", false, true, ["Exploration", "Development", "Production operations", "Subsurface", "Drilling and wells", "Projects", "Infrastructure", "New energy", "Carbon storage", "Geothermal", "Mining", "Research and technology"])
        ];

        private FieldMembershipCategoryManager(ILogger<FieldMembershipCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldMembershipCategoryManager GetInstance(ILogger<FieldMembershipCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldMembershipCategoryManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllFieldMembershipCategoryId()
        {
            EnsureDefaultCategories();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM FieldMembershipCategoryTable";
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
                _logger.LogError(ex, "Impossible to get IDs from FieldMembershipCategoryTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllFieldMembershipCategoryMetaInfo()
        {
            EnsureDefaultCategories();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM FieldMembershipCategoryTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from FieldMembershipCategoryTable");
                return null;
            }
        }

        public Model.FieldMembershipCategory? GetFieldMembershipCategoryById(Guid guid)
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
            command.CommandText = $"SELECT FieldMembershipCategory FROM FieldMembershipCategoryTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.FieldMembershipCategory? data = JsonSerializer.Deserialize<Model.FieldMembershipCategory>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned FieldMembershipCategory has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldMembershipCategory from FieldMembershipCategoryTable");
            }

            return null;
        }

        public List<Model.FieldMembershipCategory?>? GetAllFieldMembershipCategory()
        {
            EnsureDefaultCategories();
            List<Model.FieldMembershipCategory?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FieldMembershipCategory FROM FieldMembershipCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.FieldMembershipCategory>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldMembershipCategory from FieldMembershipCategoryTable");
                return null;
            }
        }

        public bool AddFieldMembershipCategory(Model.FieldMembershipCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty || GetFieldMembershipCategoryById(data.MetaInfo.ID) != null)
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
                PrepareCategory(data);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                data.CreationDate ??= now;
                data.LastModificationDate = now;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO FieldMembershipCategoryTable (" +
                    "ID, MetaInfo, Name, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, FieldMembershipCategory" +
                    ") VALUES (" +
                    $"'{data.MetaInfo.ID}', '{metaInfo}', '{data.Name}', {(data.IsExclusive ? 1 : 0)}, {(data.HasValidityPeriod ? 1 : 0)}, '{creationDate}', '{lastModificationDate}', '{serialized}')";
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
                _logger.LogError(ex, "Impossible to add FieldMembershipCategory");
                return false;
            }
        }

        public bool UpdateFieldMembershipCategoryById(Guid guid, Model.FieldMembershipCategory? data)
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
                PrepareCategory(data);
                data.LastModificationDate = DateTimeOffset.UtcNow;
                string metaInfo = JsonSerializer.Serialize(data.MetaInfo, JsonSettings.Options);
                string serialized = JsonSerializer.Serialize(data, JsonSettings.Options);
                string? creationDate = data.CreationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                string? lastModificationDate = data.LastModificationDate?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);
                var command = connection.CreateCommand();
                command.CommandText = $"UPDATE FieldMembershipCategoryTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"IsExclusive = {(data.IsExclusive ? 1 : 0)}, " +
                    $"HasValidityPeriod = {(data.HasValidityPeriod ? 1 : 0)}, " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"FieldMembershipCategory = '{serialized}' " +
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
                _logger.LogError(ex, "Impossible to update FieldMembershipCategory");
                return false;
            }
        }

        public bool DeleteFieldMembershipCategoryById(Guid guid)
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
                command.CommandText = $"DELETE FROM FieldMembershipCategoryTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete FieldMembershipCategory");
                return false;
            }
        }

        private void EnsureDefaultCategories()
        {
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM FieldMembershipCategoryTable";
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
                _logger.LogError(ex, "Impossible to count FieldMembershipCategoryTable");
                return;
            }

            foreach (DefaultFieldMembershipCategory defaultCategory in DefaultCategories)
            {
                AddFieldMembershipCategory(CreateDefaultCategory(defaultCategory));
            }
        }

        private static Model.FieldMembershipCategory CreateDefaultCategory(DefaultFieldMembershipCategory defaultCategory) =>
            new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = defaultCategory.Name,
                IsExclusive = defaultCategory.IsExclusive,
                HasValidityPeriod = defaultCategory.HasValidityPeriod,
                Options = defaultCategory.Options
                    .Select(option => new Model.FieldMembershipOption { ID = Guid.NewGuid(), Name = option })
                    .ToList()
            };

        private static void PrepareCategory(Model.FieldMembershipCategory category)
        {
            category.Options ??= [];
            foreach (Model.FieldMembershipOption option in category.Options)
            {
                if (option.ID == Guid.Empty)
                {
                    option.ID = Guid.NewGuid();
                }
            }
        }

        private sealed record DefaultFieldMembershipCategory(
            string Name,
            bool IsExclusive,
            bool HasValidityPeriod,
            string[] Options);
    }
}
