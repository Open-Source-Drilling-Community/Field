# Field Service

The ASP.NET Core service persists Field-owned records and exposes synchronous,
stateless field coordinate conversion. Conversion requests and results are never
stored.

## Runtime configuration

- `EarthCartographicProjectionHostURL`: base host for
  `/EarthCartographicProjection/api/`.
- `EarthGeodesyHostURL`: base host for `/EarthGeodesy/api/`.
- `FIELD_EXTERNAL_CONFIG`: optional path overriding the default external
  configuration file `../home/Field.Service.json` (`/home/Field.Service.json`
  in the container).
- `FIELD_PROJECTION_MAPPINGS_CONFIG`: optional path overriding the dedicated
  migration mapping file `../home/Field.ProjectionMappings.json`
  (`/home/Field.ProjectionMappings.json` in the container).
- `McpHub`: optional MCP hub registration settings.
- `ProjectionDefinitionIdMappings`: reviewed legacy projection UUID to new
  EarthCartographicProjection definition UUID mappings used only by schema-v2
  startup migration.

Example projection-mapping configuration:

```json
{
  "ProjectionDefinitionIdMappings": {
    "acf0f15f-8ed0-4030-ad9c-3e4e8d7cd29b": "30000000-0000-4000-8000-000000023031"
  }
}
```

The Helm chart accepts the inner mapping object as
`projectionDefinitionIdMappings` and mounts the generated file separately
from the Field database persistent volume.

Environment-specific reviewed files and the live verification report are in
`migration/projection-references/`.

## REST API

The service uses path base `/field/api` (routing is case-insensitive in the
provided ingress).

- `Field`: Field CRUD. A Field optionally carries `ProjectionDefinitionID`,
  which identifies an EarthCartographicProjection definition.
- `Field/BatchExport`: creates a read-only, versioned JSON backup document for
  every field or an explicitly ordered UUID selection. Selected exports are
  atomic: an invalid, duplicate, or missing UUID returns a stable error envelope
  and no partial document. `All` exports are ordered by UUID.
- `Field/BatchRestore`: validates and restores a version-1 batch-export
  document in one SQLite transaction. `FailIfExists` rejects the complete batch
  on any existing field UUID; `ReplaceExisting` atomically inserts new fields
  and replaces existing fields. No partial restore is committed.
- `FieldCoordinateConversion/Forward`: converts an ordered geographic batch
  in the projection datum or WGS 84 to canonical easting/northing.
- `FieldCoordinateConversion/Inverse`: converts canonical easting/northing to
  projection-datum coordinates and, when a usable EarthGeodesy path exists,
  WGS 84 coordinates.
- `FieldFeatureCategory`, `FieldMembershipCategory`, `FieldIdentity`, and
  `FieldDelineationLineType`: Field-owned catalog CRUD.
- `FieldUsageStatistics`: REST usage counters.

Angles are SI radians, distances are SI metres, easting precedes northing, and
batches are atomic. REST accepts at most 10,000 positions. A missing optional
datum-to-WGS-84 path yields a warning and nullable WGS-84 output; a required
WGS-84-to-projection-datum path failure rejects the batch.

Swagger is available at `/Field/api/swagger` and the merged contract at
`/Field/api/swagger/merged/swagger.json`.

## MCP

- Streamable HTTP: `/Field/api/mcp`
- WebSocket: `/Field/api/mcp/ws`
- Stateless conversion: `field_forward_convert_coordinates` and
  `field_inverse_convert_coordinates` (maximum 1,000 positions per call)
- CRUD tool groups: `field_...`, `field_feature_category_...`,
  `field_membership_category_...`, `field_identity_...`, and
  `field_delineation_line_type_...`
- Usage: `field_usage_statistics_get`

Tool names use underscores only and publish explicit JSON input schemas.

## Persistence migration safety

Schema version 2 removes the obsolete persisted calculation-case table and
renames the projection reference inside stored Field JSON. Startup first plans
the complete migration. If any non-empty legacy `CartographicProjectionID`
lacks an explicit mapping, startup fails and changes nothing. A valid plan
creates `Field.pre-v2.<UTC>.db` alongside `Field.db`, then updates all affected
Field rows and drops the obsolete table in one SQLite transaction. It does not
change Field IDs, names, timestamps, assignments, delineation lines, or other
catalog tables.

## Build and run

```bash
dotnet restore
dotnet build Field.sln
dotnet run --project Service
```

Docker example:

```bash
docker build -t field-service -f Service/Dockerfile .
docker run --rm -p 5002:5002 \
  -e ASPNETCORE_URLS="http://+:5002" \
  -e EarthCartographicProjectionHostURL="https://dev.digiwells.no/" \
  -e EarthGeodesyHostURL="https://dev.digiwells.no/" \
  -v field-home:/home field-service
```

## Contract generation

After changing dependency or Field contracts:

```bash
dotnet run --project ModelSharedIn
dotnet build Service/Service.csproj
dotnet run --project ModelSharedOut
```

## Contributors

- Eric Cayeux, NORCE Research
