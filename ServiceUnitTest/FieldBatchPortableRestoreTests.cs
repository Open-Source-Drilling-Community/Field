using System.Text.Json;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Service;
using FieldModel = OSDC.Drilling.Field.Model.Field;
using Model = OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.ServiceUnitTest;

public class FieldBatchPortableRestoreTests
{
    [Test]
    public void Export_IncludesOnlyReferencedCatalogDependencyClosure()
    {
        Guid fieldId = Guid.NewGuid(), categoryId = Guid.NewGuid(), selectedOptionId = Guid.NewGuid(), unusedOptionId = Guid.NewGuid();
        FieldModel field = new()
        {
            MetaInfo = new MetaInfo { ID = fieldId },
            FieldFeatureAssignments =
            [
                new Model.FieldFeatureAssignment { ID = Guid.NewGuid(), FeatureCategoryID = categoryId, FeatureOptionID = selectedOptionId }
            ]
        };
        Model.FieldFeatureCategory category = new()
        {
            MetaInfo = new MetaInfo { ID = categoryId }, Name = "Resource", Options =
            [
                new Model.FieldFeatureOption { ID = selectedOptionId, Name = "Oil" },
                new Model.FieldFeatureOption { ID = unusedOptionId, Name = "Gas" }
            ]
        };

        FieldBatchExportOutcome outcome = FieldBatchExporter.Create(
            new Model.FieldBatchExportRequest { Scope = Model.FieldBatchExportScope.All }, [field], DateTimeOffset.UtcNow,
            [category]);

        Assert.That(outcome.IsSuccess, Is.True, outcome.Error?.Message);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Document!.SchemaVersion, Is.EqualTo(2));
            Assert.That(outcome.Document.CatalogDependencies!.FeatureCategories, Has.Count.EqualTo(1));
            Assert.That(outcome.Document.CatalogDependencies.FeatureCategories.Single().Options!.Select(value => value.ID),
                Is.EqualTo(new[] { selectedOptionId }));
        });
    }

    [Test]
    public void Restore_MapsEquivalentDelineationTypeByName()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid localTypeId = Guid.NewGuid();
        InsertLineType(connection, localTypeId, "  FIELD   delineation ");
        Guid sourceTypeId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(connection,
            Request(fieldId, sourceTypeId, "field delineation", Model.FieldBatchCatalogRestorePolicy.MapExisting),
            DateTimeOffset.UtcNow);

        Assert.That(outcome.IsSuccess, Is.True, outcome.Error?.Message);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Response!.CreatedCatalogDefinitionCount, Is.Zero);
            Assert.That(outcome.Response.CatalogMappings.Single().LocalID, Is.EqualTo(localTypeId));
        });
        FieldModel restored = ReadField(connection, fieldId);
        Assert.That(restored.DelineationLines!.Single().DelineationLineTypeID, Is.EqualTo(localTypeId));
    }

    [Test]
    public void Restore_FieldConflict_RollsBackMissingCatalogCreation()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid sourceTypeId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        InsertField(connection, new FieldModel { MetaInfo = new MetaInfo { ID = fieldId }, Name = "existing" });

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(connection,
            Request(fieldId, sourceTypeId, "new portable type", Model.FieldBatchCatalogRestorePolicy.MapOrCreateMissing),
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(FieldBatchRestoreFailureKind.Conflict));
            Assert.That(Count(connection, "FieldDelineationLineTypeTable"), Is.Zero);
            Assert.That(ReadField(connection, fieldId).Name, Is.EqualTo("existing"));
        });
    }

    [Test]
    public void Restore_CreatesMissingDefinitionWithLocalUuidAndRewritesField()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid sourceTypeId = Guid.NewGuid(), fieldId = Guid.NewGuid();

        FieldBatchRestoreOutcome outcome = FieldBatchRestorer.Restore(connection,
            Request(fieldId, sourceTypeId, "new portable type", Model.FieldBatchCatalogRestorePolicy.MapOrCreateMissing),
            DateTimeOffset.UtcNow);

        Assert.That(outcome.IsSuccess, Is.True, outcome.Error?.Message);
        Model.FieldBatchCatalogMapping mapping = outcome.Response!.CatalogMappings.Single();
        Assert.Multiple(() =>
        {
            Assert.That(mapping.LocalID, Is.Not.EqualTo(sourceTypeId));
            Assert.That(mapping.Resolution, Is.EqualTo("created"));
            Assert.That(outcome.Response.CreatedCatalogDefinitionCount, Is.EqualTo(1));
            Assert.That(ReadField(connection, fieldId).DelineationLines!.Single().DelineationLineTypeID,
                Is.EqualTo(mapping.LocalID));
        });
    }

    private static Model.FieldBatchRestoreRequest Request(Guid fieldId, Guid typeId, string typeName,
        Model.FieldBatchCatalogRestorePolicy catalogPolicy) => new()
    {
        ConflictPolicy = Model.FieldBatchRestoreConflictPolicy.FailIfExists,
        CatalogPolicy = catalogPolicy,
        Document = new Model.FieldBatchExportDocument
        {
            ExportedAtUtc = DateTimeOffset.UtcNow,
            CatalogDependencies = new Model.FieldBatchCatalogDependencies
            {
                DelineationLineTypes =
                [
                    new Model.FieldDelineationLineType { MetaInfo = new MetaInfo { ID = typeId }, Name = typeName }
                ]
            },
            Fields =
            [
                new FieldModel
                {
                    MetaInfo = new MetaInfo { ID = fieldId }, Name = "portable field",
                    DelineationLines = [new Model.FieldDelineationLine { ID = Guid.NewGuid(), DelineationLineTypeID = typeId }]
                }
            ]
        }
    };

    private static SqliteConnection OpenDatabase()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE FieldTable (ID TEXT PRIMARY KEY, MetaInfo TEXT NOT NULL, Field TEXT NOT NULL);
            CREATE TABLE FieldFeatureCategoryTable (ID TEXT PRIMARY KEY, MetaInfo TEXT, Name TEXT, IsExclusive INTEGER, HasValidityPeriod INTEGER, CreationDate TEXT, LastModificationDate TEXT, FieldFeatureCategory TEXT);
            CREATE TABLE FieldMembershipCategoryTable (ID TEXT PRIMARY KEY, MetaInfo TEXT, Name TEXT, IsExclusive INTEGER, HasValidityPeriod INTEGER, CreationDate TEXT, LastModificationDate TEXT, FieldMembershipCategory TEXT);
            CREATE TABLE FieldIdentityTable (ID TEXT PRIMARY KEY, MetaInfo TEXT, Name TEXT, CreationDate TEXT, LastModificationDate TEXT, FieldIdentity TEXT);
            CREATE TABLE FieldDelineationLineTypeTable (ID TEXT PRIMARY KEY, MetaInfo TEXT, Name TEXT, CreationDate TEXT, LastModificationDate TEXT, FieldDelineationLineType TEXT);
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static void InsertLineType(SqliteConnection connection, Guid id, string name)
    {
        Model.FieldDelineationLineType value = new() { MetaInfo = new MetaInfo { ID = id }, Name = name };
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldDelineationLineTypeTable (ID,MetaInfo,Name,FieldDelineationLineType) VALUES ($id,$meta,$name,$doc)";
        command.Parameters.AddWithValue("$id", id.ToString()); command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo));
        command.Parameters.AddWithValue("$name", name); command.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(value)); command.ExecuteNonQuery();
    }

    private static void InsertField(SqliteConnection connection, FieldModel field)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldTable (ID,MetaInfo,Field) VALUES ($id,$meta,$doc)";
        command.Parameters.AddWithValue("$id", field.MetaInfo!.ID.ToString()); command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(field.MetaInfo));
        command.Parameters.AddWithValue("$doc", JsonSerializer.Serialize(field)); command.ExecuteNonQuery();
    }

    private static FieldModel ReadField(SqliteConnection connection, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand(); command.CommandText = "SELECT Field FROM FieldTable WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString()); return JsonSerializer.Deserialize<FieldModel>((string)command.ExecuteScalar()!)!;
    }

    private static long Count(SqliteConnection connection, string table)
    { using SqliteCommand command = connection.CreateCommand(); command.CommandText = $"SELECT COUNT(*) FROM {table}"; return (long)command.ExecuteScalar()!; }
}
