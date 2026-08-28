using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Service;
using OSDC.Drilling.Field.Service.Managers;
using Model = OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.ServiceUnitTest;

public sealed class FieldIntegrityAndConcurrencyTests
{
    private string _databasePath = null!;
    private SqlConnectionManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"field-integrity-{Guid.NewGuid():N}.db");
        _manager = new SqlConnectionManager($"Data Source={_databasePath}", NullLogger<SqlConnectionManager>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
    }

    [Test]
    public void DeleteIdentity_RejectsReferencedDefinitionWithoutChangingData()
    {
        Guid identityId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        InsertIdentity(new Model.FieldIdentity
        {
            MetaInfo = new MetaInfo { ID = identityId }, Name = "Official name",
            CreationDate = now, LastModificationDate = now
        });
        InsertField(new Model.Field
        {
            MetaInfo = new MetaInfo { ID = fieldId }, CreationDate = now, LastModificationDate = now,
            FieldIdentityAssignments = [new Model.FieldIdentityAssignment { ID = Guid.NewGuid(), IdentityID = identityId, Value = "Test" }]
        });

        FieldMutationResult result = FieldCatalogMutationManager.DeleteIdentity(
            _manager, NullLogger.Instance, identityId);

        Assert.Multiple(() =>
        {
            Assert.That(result.FailureKind, Is.EqualTo(FieldMutationFailureKind.Conflict));
            Assert.That(result.Error?.Error, Is.EqualTo("reference_conflict"));
            Assert.That(result.Error?.Errors.Single().ReferencingFieldIDs, Is.EqualTo(new[] { fieldId }));
            Assert.That(RowExists("FieldIdentityTable", identityId), Is.True);
        });
    }

    [Test]
    public void UpdateIdentity_UsesServerTimestampAndRejectsStaleWriter()
    {
        Guid identityId = Guid.NewGuid();
        DateTimeOffset created = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset originalModified = DateTimeOffset.UtcNow.AddMinutes(-1);
        InsertIdentity(new Model.FieldIdentity
        {
            MetaInfo = new MetaInfo { ID = identityId }, Name = "Original",
            CreationDate = created, LastModificationDate = originalModified
        });

        Model.FieldIdentity first = new() { MetaInfo = new MetaInfo { ID = identityId }, Name = "First" };
        FieldMutationResult firstResult = FieldCatalogMutationManager.UpdateIdentity(
            _manager, NullLogger.Instance, identityId, originalModified, first);
        Model.FieldIdentity stale = new() { MetaInfo = new MetaInfo { ID = identityId }, Name = "Stale" };
        FieldMutationResult staleResult = FieldCatalogMutationManager.UpdateIdentity(
            _manager, NullLogger.Instance, identityId, originalModified, stale);

        Model.FieldIdentity stored = ReadIdentity(identityId);
        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Succeeded, Is.True);
            Assert.That(first.CreationDate, Is.EqualTo(created));
            Assert.That(first.LastModificationDate, Is.GreaterThan(originalModified));
            Assert.That(staleResult.FailureKind, Is.EqualTo(FieldMutationFailureKind.Conflict));
            Assert.That(staleResult.Error?.Error, Is.EqualTo("concurrency_conflict"));
            Assert.That(stored.Name, Is.EqualTo("First"));
            Assert.That(stored.LastModificationDate, Is.EqualTo(first.LastModificationDate));
        });
    }

    [Test]
    public void ValidateField_RejectsOptionThatDoesNotBelongToSelectedCategory()
    {
        Guid categoryId = Guid.NewGuid();
        Guid knownOptionId = Guid.NewGuid();
        InsertFeatureCategory(new Model.FieldFeatureCategory
        {
            MetaInfo = new MetaInfo { ID = categoryId }, Name = "Resource",
            Options = [new Model.FieldFeatureOption { ID = knownOptionId, Name = "Oil" }]
        });
        Model.Field field = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
            FieldFeatureAssignments =
            [
                new Model.FieldFeatureAssignment
                {
                    ID = Guid.NewGuid(), FeatureCategoryID = categoryId, FeatureOptionID = Guid.NewGuid()
                }
            ]
        };

        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteTransaction transaction = connection.BeginTransaction();
        List<Model.FieldMutationError> errors = FieldReferenceIntegrityValidator.ValidateField(connection, transaction, field);
        transaction.Rollback();

        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(errors[0].Property, Is.EqualTo("FieldFeatureAssignments[0].FeatureOptionID"));
            Assert.That(errors[0].Code, Is.EqualTo("option_not_in_category"));
        });
    }

    [Test]
    public void UpdateFeatureCategory_RejectsRemovingAnOptionUsedByAField()
    {
        Guid categoryId = Guid.NewGuid();
        Guid optionId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        DateTimeOffset modified = DateTimeOffset.UtcNow.AddMinutes(-1);
        InsertFeatureCategory(new Model.FieldFeatureCategory
        {
            MetaInfo = new MetaInfo { ID = categoryId }, Name = "Resource",
            CreationDate = modified, LastModificationDate = modified,
            Options = [new Model.FieldFeatureOption { ID = optionId, Name = "Oil" }]
        });
        InsertField(new Model.Field
        {
            MetaInfo = new MetaInfo { ID = fieldId },
            FieldFeatureAssignments =
            [
                new Model.FieldFeatureAssignment
                {
                    ID = Guid.NewGuid(), FeatureCategoryID = categoryId, FeatureOptionID = optionId
                }
            ]
        });

        Model.FieldFeatureCategory replacement = new()
        {
            MetaInfo = new MetaInfo { ID = categoryId }, Name = "Resource", Options = []
        };
        FieldMutationResult result = FieldCatalogMutationManager.UpdateFeatureCategory(
            _manager, NullLogger.Instance, categoryId, modified, replacement);

        Assert.Multiple(() =>
        {
            Assert.That(result.FailureKind, Is.EqualTo(FieldMutationFailureKind.Conflict));
            Assert.That(result.Error?.Error, Is.EqualTo("reference_conflict"));
            Assert.That(result.Error?.Errors.Single().Code, Is.EqualTo("catalog_option_in_use"));
            Assert.That(result.Error?.Errors.Single().ReferencingFieldIDs, Is.EqualTo(new[] { fieldId }));
        });
    }

    private void InsertIdentity(Model.FieldIdentity value)
    {
        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldIdentityTable (ID,MetaInfo,Name,CreationDate,LastModificationDate,FieldIdentity) VALUES ($id,$meta,$name,$created,$modified,$document)";
        command.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$name", value.Name!);
        command.Parameters.AddWithValue("$created", value.CreationDate!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        command.Parameters.AddWithValue("$modified", value.LastModificationDate!.Value.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
        command.ExecuteNonQuery();
    }

    private void InsertFeatureCategory(Model.FieldFeatureCategory value)
    {
        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldFeatureCategoryTable (ID,MetaInfo,Name,IsExclusive,HasValidityPeriod,FieldFeatureCategory) VALUES ($id,$meta,$name,0,0,$document)";
        command.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$name", value.Name!);
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
        command.ExecuteNonQuery();
    }

    private void InsertField(Model.Field value)
    {
        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO FieldTable (ID,MetaInfo,Field) VALUES ($id,$meta,$document)";
        command.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
        command.ExecuteNonQuery();
    }

    private bool RowExists(string table, Guid id)
    {
        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private Model.FieldIdentity ReadIdentity(Guid id)
    {
        using SqliteConnection connection = _manager.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT FieldIdentity FROM FieldIdentityTable WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return JsonSerializer.Deserialize<Model.FieldIdentity>((string)command.ExecuteScalar()!, JsonSettings.Options)!;
    }
}
