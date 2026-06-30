using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NORCE.Drilling.Field.Service.Managers
{
    public class FieldFeatureCategoryManager
    {
        private static FieldFeatureCategoryManager? _instance;
        private readonly ILogger<FieldFeatureCategoryManager> _logger;
        private readonly SqlConnectionManager _connectionManager;
        private static readonly DefaultFieldFeatureCategory[] DefaultCategories =
        [
            new(
                "Primary Resource",
                "Primary resource or purpose represented by the field.",
                true,
                false,
                [
                    "Hydrocarbon",
                    "Geothermal heat",
                    "Groundwater",
                    "Mineral brine",
                    "Solid mineral deposit",
                    "Gas storage",
                    "CO2 storage",
                    "Hydrogen storage",
                    "Waste disposal"
                ]),
            new(
                "Reservoir Fluid",
                "Fluids or mobile resources associated with the field.",
                false,
                false,
                [
                    "Oil",
                    "Gas",
                    "Condensate",
                    "Water / brine",
                    "CO2",
                    "Hydrogen",
                    "Helium",
                    "Steam",
                    "Geothermal fluid",
                    "Mineralized brine"
                ]),
            new(
                "Reservoir / Host Rock Lithology",
                "Dominant reservoir or host-rock lithology classes.",
                false,
                false,
                [
                    "Clastic sedimentary rock",
                    "Carbonate rock",
                    "Evaporite",
                    "Organic-rich shale",
                    "Coal",
                    "Crystalline basement",
                    "Volcanic rock",
                    "Metamorphic rock",
                    "Unconsolidated sediment",
                    "Mixed lithology"
                ]),
            new(
                "Storage / Flow System",
                "Main storage and flow characteristics of the field.",
                false,
                false,
                [
                    "Matrix dominated",
                    "Fracture influenced",
                    "Fracture dominated",
                    "Karst / vuggy",
                    "Tight / low permeability",
                    "Unconsolidated",
                    "Compartmentalized",
                    "Layered / stratified",
                    "Dual-porosity"
                ]),
            new(
                "Development / Lifecycle Status",
                "Lifecycle status of the field over time.",
                true,
                true,
                [
                    "Prospect",
                    "Discovery",
                    "Appraisal",
                    "Development planning",
                    "Under development",
                    "Producing / operating",
                    "Suspended",
                    "Depleted",
                    "Abandoned",
                    "Monitoring / post-closure"
                ]),
            new(
                "Primary Recovery / Operating Mode",
                "Recovery, extraction, injection, or operating modes applied to the field.",
                false,
                true,
                [
                    "Primary depletion",
                    "Water injection",
                    "Gas injection",
                    "CO2 injection",
                    "Steam / thermal injection",
                    "Chemical injection",
                    "Hydraulic stimulation",
                    "Acid stimulation",
                    "Geothermal production",
                    "Geothermal reinjection",
                    "Brine extraction",
                    "Solution mining",
                    "In-situ leaching",
                    "Conventional mining"
                ]),
            new(
                "Drive / Support Mechanism",
                "Natural or artificial mechanisms supporting production or pressure.",
                false,
                true,
                [
                    "Solution gas drive",
                    "Gas cap drive",
                    "Aquifer support",
                    "Waterflood support",
                    "Gas injection support",
                    "Compaction drive",
                    "Gravity drainage",
                    "Thermal drive",
                    "Pressure depletion"
                ]),
            new(
                "Trap / Containment Style",
                "Trap, closure, or containment mechanisms relevant to the field.",
                false,
                false,
                [
                    "Structural closure",
                    "Stratigraphic trap",
                    "Fault-bounded trap",
                    "Salt-related trap",
                    "Hydrodynamic trap",
                    "Caprock containment",
                    "Engineered containment",
                    "Open / unconfined system",
                    "Uncertain containment"
                ]),
            new(
                "Pressure / Temperature Regime",
                "Pressure and temperature regime descriptors.",
                false,
                true,
                [
                    "Normal pressure",
                    "Overpressured",
                    "Depleted pressure",
                    "High pressure",
                    "Low temperature",
                    "High temperature",
                    "HPHT",
                    "Supercritical conditions"
                ]),
            new(
                "Fluid / Material Quality",
                "Fluid, brine, mineral, or material quality indicators.",
                false,
                true,
                [
                    "Sour / H2S bearing",
                    "CO2 rich",
                    "High salinity",
                    "Heavy oil",
                    "Wax prone",
                    "Asphaltene prone",
                    "Scaling prone",
                    "Corrosive fluid",
                    "Radioactive / NORM risk",
                    "Critical-mineral bearing"
                ]),
            new(
                "Geothermal System Type",
                "Primary geothermal system classification.",
                true,
                false,
                [
                    "Hydrothermal",
                    "Enhanced geothermal system",
                    "Closed-loop geothermal",
                    "Sedimentary geothermal",
                    "Geopressured geothermal",
                    "Magmatic / volcanic geothermal",
                    "Mine-water geothermal"
                ]),
            new(
                "Geothermal Use",
                "Geothermal utilization modes.",
                false,
                true,
                [
                    "Power generation",
                    "District heating",
                    "Industrial heat",
                    "Direct use",
                    "Cooling",
                    "Thermal storage",
                    "Hybrid heat and power"
                ]),
            new(
                "Mining Deposit Type",
                "Primary mining deposit style.",
                true,
                false,
                [
                    "Vein deposit",
                    "Disseminated deposit",
                    "Massive sulfide",
                    "Porphyry deposit",
                    "Skarn",
                    "Sedimentary deposit",
                    "Evaporite deposit",
                    "Placer deposit",
                    "Coal deposit",
                    "Brine deposit",
                    "Laterite deposit"
                ]),
            new(
                "Mining Commodity Group",
                "Commodity groups associated with a mining field or deposit.",
                false,
                false,
                [
                    "Precious metals",
                    "Base metals",
                    "Critical minerals",
                    "Industrial minerals",
                    "Energy minerals",
                    "Salts / evaporites",
                    "Aggregates",
                    "Coal",
                    "Uranium",
                    "Rare earth elements"
                ]),
            new(
                "Operating Environment",
                "Operational and geographic environment descriptors.",
                false,
                false,
                [
                    "Onshore",
                    "Offshore",
                    "Deepwater",
                    "Arctic / cold region",
                    "Desert",
                    "Urban / populated area",
                    "Environmentally sensitive area",
                    "Protected area",
                    "Cross-border",
                    "Remote location"
                ]),
            new(
                "Data / Knowledge Maturity",
                "Maturity and confidence level of field knowledge.",
                true,
                true,
                [
                    "Conceptual",
                    "Sparse data",
                    "Appraisal data",
                    "Development model",
                    "Mature model",
                    "History matched",
                    "Uncertain / disputed interpretation"
                ])
        ];

        private FieldFeatureCategoryManager(ILogger<FieldFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static FieldFeatureCategoryManager GetInstance(ILogger<FieldFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new FieldFeatureCategoryManager(logger, connectionManager);
            return _instance;
        }

        public List<Guid>? GetAllFieldFeatureCategoryId()
        {
            EnsureDefaultCategories();
            List<Guid> ids = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ID FROM FieldFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get IDs from FieldFeatureCategoryTable");
                return null;
            }
        }

        public List<MetaInfo?>? GetAllFieldFeatureCategoryMetaInfo()
        {
            EnsureDefaultCategories();
            List<MetaInfo?> metaInfos = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MetaInfo FROM FieldFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to get MetaInfo from FieldFeatureCategoryTable");
                return null;
            }
        }

        public Model.FieldFeatureCategory? GetFieldFeatureCategoryById(Guid guid)
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
            command.CommandText = $"SELECT FieldFeatureCategory FROM FieldFeatureCategoryTable WHERE ID = '{guid}'";
            try
            {
                using var reader = command.ExecuteReader();
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    Model.FieldFeatureCategory? data = JsonSerializer.Deserialize<Model.FieldFeatureCategory>(reader.GetString(0), JsonSettings.Options);
                    if (data != null && data.MetaInfo != null && data.MetaInfo.ID != guid)
                    {
                        throw new SqliteException("SQLite database corrupted: returned FieldFeatureCategory has the wrong ID.", 1);
                    }
                    return data;
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldFeatureCategory from FieldFeatureCategoryTable");
            }

            return null;
        }

        public List<Model.FieldFeatureCategory?>? GetAllFieldFeatureCategory()
        {
            EnsureDefaultCategories();
            List<Model.FieldFeatureCategory?> values = [];
            var connection = _connectionManager.GetConnection();
            if (connection == null)
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FieldFeatureCategory FROM FieldFeatureCategoryTable";
            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read() && !reader.IsDBNull(0))
                {
                    values.Add(JsonSerializer.Deserialize<Model.FieldFeatureCategory>(reader.GetString(0), JsonSettings.Options));
                }
                return values;
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "Impossible to get FieldFeatureCategory from FieldFeatureCategoryTable");
                return null;
            }
        }

        public bool AddFieldFeatureCategory(Model.FieldFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return false;
            }
            if (GetFieldFeatureCategoryById(data.MetaInfo.ID) != null)
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
                command.CommandText = "INSERT INTO FieldFeatureCategoryTable (" +
                    "ID, MetaInfo, Name, Description, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, FieldFeatureCategory" +
                    ") VALUES (" +
                    $"'{data.MetaInfo.ID}', '{metaInfo}', '{data.Name}', '{data.Description}', {(data.IsExclusive ? 1 : 0)}, {(data.HasValidityPeriod ? 1 : 0)}, '{creationDate}', '{lastModificationDate}', '{serialized}')";
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
                _logger.LogError(ex, "Impossible to add FieldFeatureCategory");
                return false;
            }
        }

        public bool UpdateFieldFeatureCategoryById(Guid guid, Model.FieldFeatureCategory? data)
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
                command.CommandText = $"UPDATE FieldFeatureCategoryTable SET " +
                    $"MetaInfo = '{metaInfo}', " +
                    $"Name = '{data.Name}', " +
                    $"Description = '{data.Description}', " +
                    $"IsExclusive = {(data.IsExclusive ? 1 : 0)}, " +
                    $"HasValidityPeriod = {(data.HasValidityPeriod ? 1 : 0)}, " +
                    $"CreationDate = '{creationDate}', " +
                    $"LastModificationDate = '{lastModificationDate}', " +
                    $"FieldFeatureCategory = '{serialized}' " +
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
                _logger.LogError(ex, "Impossible to update FieldFeatureCategory");
                return false;
            }
        }

        public bool DeleteFieldFeatureCategoryById(Guid guid)
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
                command.CommandText = $"DELETE FROM FieldFeatureCategoryTable WHERE ID = '{guid}'";
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (SqliteException ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Impossible to delete FieldFeatureCategory");
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
            command.CommandText = "SELECT COUNT(*) FROM FieldFeatureCategoryTable";
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
                _logger.LogError(ex, "Impossible to count FieldFeatureCategoryTable");
                return;
            }

            foreach (DefaultFieldFeatureCategory defaultCategory in DefaultCategories)
            {
                AddFieldFeatureCategory(CreateDefaultCategory(defaultCategory));
            }
        }

        private static Model.FieldFeatureCategory CreateDefaultCategory(DefaultFieldFeatureCategory defaultCategory) =>
            new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = defaultCategory.Name,
                Description = defaultCategory.Description,
                IsExclusive = defaultCategory.IsExclusive,
                HasValidityPeriod = defaultCategory.HasValidityPeriod,
                Options = defaultCategory.Options
                    .Select(option => new Model.FieldFeatureOption { ID = Guid.NewGuid(), Name = option })
                    .ToList()
            };

        private static void PrepareCategory(Model.FieldFeatureCategory category)
        {
            category.Options ??= [];
            foreach (Model.FieldFeatureOption option in category.Options)
            {
                if (option.ID == Guid.Empty)
                {
                    option.ID = Guid.NewGuid();
                }
            }
        }

        private sealed record DefaultFieldFeatureCategory(
            string Name,
            string Description,
            bool IsExclusive,
            bool HasValidityPeriod,
            string[] Options);
    }
}
