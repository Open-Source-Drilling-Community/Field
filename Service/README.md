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
- `McpHub`: optional MCP hub registration settings.

## REST API

The service uses path base `/Field/api`.

- `Field`: Field CRUD. A Field optionally carries `ProjectionDefinitionID`,
  which identifies an EarthCartographicProjection definition. Create and update
  validate all Field-owned catalog references in the same transaction.
- `Field/BatchExport`: creates a read-only, versioned JSON backup document for
  every field or an explicitly ordered UUID selection. Selected exports are
  atomic: an invalid, duplicate, or missing UUID returns a stable error envelope
  and no partial document. `All` exports are ordered by UUID.
- `Field/BatchRestore`: validates and restores a version-2 batch-export
  document in one SQLite transaction. `FailIfExists` rejects the complete batch
  on any existing field UUID; `ReplaceExisting` atomically inserts new fields
  and replaces existing fields. `MapExisting` resolves referenced catalog
  records by compatible UUID or unique normalized name, while
  `MapOrCreateMissing` may create missing local records with server-generated
  UUIDs. Catalog mapping, reference rewriting, optional catalog creation, and
  every field write are atomic.
- `FieldCoordinateConversion/Forward`: converts an ordered geographic batch
  in the projection datum or WGS 84 to canonical easting/northing.
- `FieldCoordinateConversion/Inverse`: converts canonical easting/northing to
  projection-datum coordinates and, when a usable EarthGeodesy path exists,
  WGS 84 coordinates.
- `FieldFeatureCategory`, `FieldMembershipCategory`, `FieldIdentity`, and
  `FieldDelineationLineType`: Field-owned catalog CRUD. Deletion is rejected
  while a Field references a definition. Category updates are rejected if they
  remove an option still referenced by a Field. Conflict errors include the
  affected Field UUIDs.
- `FieldUsageStatistics`: REST usage counters.

Angles are SI radians, distances are SI metres, easting precedes northing, and
batches are atomic. REST accepts at most 10,000 positions. A missing optional
datum-to-WGS-84 path yields a warning and nullable WGS-84 output; a required
WGS-84-to-projection-datum path failure rejects the batch.

Swagger is available at `/Field/api/swagger` and the merged contract at
`/Field/api/swagger/merged/swagger.json`.

All PUT endpoints are complete replacements and require the query parameter
`expectedModifiedUtc`, copied from the latest returned `LastModificationDate`.
The server preserves `CreationDate`, assigns a new `LastModificationDate`, and
returns HTTP 409 `concurrency_conflict` if another writer has changed the record.

## MCP

- Streamable HTTP: `/Field/api/mcp`
- WebSocket: `/Field/api/mcp/ws`
- Stateless conversion: `field_forward_convert_coordinates` and
  `field_inverse_convert_coordinates` (maximum 1,000 positions per call)
- Portable transfer: `field_batch_export` and `field_batch_restore`; restore is
  marked destructive and exposes conflict and catalog-mapping policies
- CRUD tool groups: `field_...`, `field_feature_category_...`,
  `field_membership_category_...`, `field_identity_...`, and
  `field_delineation_line_type_...`

Tool names use underscores only. Tools publish resource-specific JSON input and
output schemas and MCP annotations. Successes contain schema-conforming structured
content plus a JSON text fallback; failures set `isError=true`, return the
stable `{error,message,errors}` JSON envelope, and omit success-shaped
structured content. Usage statistics remain available through REST and are
intentionally not exposed through MCP.

## Persistence compatibility

The current database schema version is 3. New databases are created at this
version. Existing schema-version-2 databases are upgraded transactionally by
initializing missing creation/modification timestamps; the database structure
is then validated before the service becomes available.

## Build and run

```bash
dotnet restore
dotnet build Field.sln
dotnet run --project Service
```

Docker example:

```bash
docker build -t digiwells/osdcdrillingfieldservice:stable -f Service/Dockerfile .
docker run --rm -p 5002:5002 \
  -e ASPNETCORE_URLS="http://+:5002" \
  -e EarthCartographicProjectionHostURL="https://dev.digiwells.no/" \
  -e EarthGeodesyHostURL="https://dev.digiwells.no/" \
  -v field-home:/home digiwells/osdcdrillingfieldservice:stable
```

The Helm chart is `Service/charts/osdcdrillingfieldservice`; its Kubernetes
Service name is `osdcfieldservice`. Set `persistence.existingClaim` to adopt an
existing `field-claim` PVC.

## Contract generation

After changing dependency or Field contracts:

```bash
dotnet run --project ModelSharedIn
dotnet build Service/Service.csproj
dotnet run --project ModelSharedOut
```

## Contributors

- Eric Cayeux, NORCE Research
