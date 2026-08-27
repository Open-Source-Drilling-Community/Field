using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace NORCE.Drilling.Field.Service.Managers;

public sealed record FieldProjectionMigrationChange(
    Guid FieldID,
    Guid LegacyCartographicProjectionID,
    Guid ProjectionDefinitionID,
    string OriginalFieldJson,
    string MigratedFieldJson);

public sealed record FieldProjectionMigrationPlan(IReadOnlyList<FieldProjectionMigrationChange> Changes)
{
    public bool HasChanges => Changes.Count > 0;
}

/// <summary>
/// Plans and applies the one-time persisted JSON reference migration. Planning is
/// deliberately fail-closed: every non-empty legacy UUID must have an explicit,
/// reviewed mapping before any row is changed.
/// </summary>
public static class FieldProjectionReferenceMigrator
{
    public static FieldProjectionMigrationPlan Plan(
        SqliteConnection connection,
        IReadOnlyDictionary<Guid, Guid> mappings)
    {
        List<FieldProjectionMigrationChange> changes = [];
        List<string> failures = [];
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ID, Field FROM FieldTable ORDER BY ID";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Guid fieldId = Guid.Parse(reader.GetString(0));
            string json = reader.GetString(1);
            JsonObject field = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException($"Field {fieldId} contains invalid JSON.");

            if (!TryReadGuid(field, "CartographicProjectionID", out Guid? legacyId))
                continue;

            TryReadGuid(field, "ProjectionDefinitionID", out Guid? currentId);
            if (legacyId is null || legacyId == Guid.Empty)
            {
                field.Remove("CartographicProjectionID");
                if (currentId is null)
                    field["ProjectionDefinitionID"] = null;
                changes.Add(new(fieldId, legacyId ?? Guid.Empty, currentId ?? Guid.Empty, json, Serialize(field)));
                continue;
            }

            if (!mappings.TryGetValue(legacyId.Value, out Guid replacement) || replacement == Guid.Empty)
            {
                failures.Add($"Field {fieldId}: no reviewed mapping for legacy projection {legacyId}");
                continue;
            }
            if (currentId is not null && currentId != Guid.Empty && currentId != replacement)
            {
                failures.Add($"Field {fieldId}: ProjectionDefinitionID {currentId} conflicts with mapped value {replacement}");
                continue;
            }

            field.Remove("CartographicProjectionID");
            field["ProjectionDefinitionID"] = replacement.ToString();
            changes.Add(new(fieldId, legacyId.Value, replacement, json, Serialize(field)));
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("Field projection-reference migration is incomplete. No rows were changed. " + string.Join("; ", failures));

        return new(changes);
    }

    public static void Apply(SqliteConnection connection, SqliteTransaction transaction, FieldProjectionMigrationPlan plan)
    {
        foreach (FieldProjectionMigrationChange change in plan.Changes)
        {
            using SqliteCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE FieldTable SET Field = $field WHERE ID = $id AND Field = $original";
            update.Parameters.AddWithValue("$field", change.MigratedFieldJson);
            update.Parameters.AddWithValue("$id", change.FieldID.ToString());
            update.Parameters.AddWithValue("$original", change.OriginalFieldJson);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Field {change.FieldID} changed while its projection reference was being migrated.");
        }
    }

    private static bool TryReadGuid(JsonObject field, string propertyName, out Guid? value)
    {
        value = null;
        if (!field.TryGetPropertyValue(propertyName, out JsonNode? node))
            return false;
        if (node is null)
            return true;
        if (!Guid.TryParse(node.GetValue<string>(), out Guid parsed))
            throw new InvalidOperationException($"Property {propertyName} is not a UUID.");
        value = parsed;
        return true;
    }

    private static string Serialize(JsonObject field) =>
        field.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
}
