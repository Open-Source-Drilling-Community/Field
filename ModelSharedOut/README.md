# ModelSharedOut Project

Generated source and merged OpenAPI outputs preserve the service's structured `x-osdc-semantic` extensions and must be refreshed after semantic binding changes.

ModelSharedOut produces the merged Field OpenAPI document and the generated C#
client/DTO contract consumed by the WebApp and tests.

## Inputs and outputs

The generator merges the OpenAPI documents in `json-schemas/`:

- `FieldFullName.json`
- `ClusterFullName.json`
- `TrajectoryFullName.json`
- `EarthCartographicProjectionModel.json`
- `EarthGeodesyModel.json`
- `EarthVerticalDatumMergedModel.json`

`TrajectoryFullName.json` is copied from Trajectory's current service-owned
Swagger artifact at `Trajectory/ModelSharedOut/json-schemas/TrajectoryFullName.json`.
Use that document rather than Trajectory's served merged aggregate, which also
contains transitive snapshots of Field, Cluster, Well, and WellBore schemas.

It writes these version-controlled outputs:

- `Service/wwwroot/json-schema/FieldMergedModel.json`, served at
  `/Field/api/swagger/merged/swagger.json`
- `ModelSharedOut/FieldMergedModel.cs`, generated in namespace
  `OSDC.Drilling.Field.ModelShared`

The generated types include Field CRUD, schema-version-2 batch export/restore
and catalog-mapping contracts, stateless forward/inverse conversion, managed
Field vocabularies, assignments, delineation data, and the selected dependency
contracts included in the merge.

## Regeneration

In Debug builds, `Service/Service.csproj` uses `dotnet swagger tofile` to refresh
`ModelSharedOut/json-schemas/FieldFullName.json`. After changing Field models or
controller contracts, run from the solution root:

```bash
dotnet build Service/Service.csproj -c Debug
dotnet run --project ModelSharedOut
```

If a dependency contract also changed, run the complete sequence:

```bash
dotnet run --project ModelSharedIn
dotnet build Service/Service.csproj -c Debug
dotnet run --project ModelSharedOut
```

Review and commit the input schemas and both generated outputs. Do not edit
`FieldMergedModel.cs` or `FieldMergedModel.json` manually.

## Usage example

```csharp
using OSDC.Drilling.Field.ModelShared;

var baseUrl = "https://localhost:5001/Field/api/";
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};
using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
var client = new Client(baseUrl, http);

var ids = await client.GetAllFieldIdAsync();
```

## Dependencies

- `Microsoft.OpenApi.Readers`: parses OpenAPI inputs
- `NSwag.CodeGeneration.CSharp`: generates the C# client and DTOs

The generator normalizes schema names to short names and writes OpenAPI 3.0.3.
Review generation errors and diffs for schema-name collisions.

## Contributors

- Eric Cayeux, NORCE Research

## Preserving semantic metadata

Refresh `FieldFullName.json` from the built Service OpenAPI output before running this generator. Refresh the sibling ClusterFullName.json input from that service's authoritative schema when its contract changes. The generator merges dependency snapshots first and `FieldFullName.json` last so older copies of shared types cannot overwrite this service's annotations.

Run the existing `ModelSharedOut` program and confirm the overwrite prompt. Commit both generated `FieldMergedModel.cs` and `Service/wwwroot/json-schema/FieldMergedModel.json` with their schema inputs. `x-osdc-semantic` and contextual `x-osdc-semantic-bindings` remain in the merged JSON contract; generated C# carries the accompanying property documentation. Do not hand-edit generated model code. Build the full solution and run contract tests after regeneration.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/Field/LightData` / `field_discover`
- `/FieldDelineationLineType/LightData` / `field_delineation_line_type_discover`
- `/FieldFeatureCategory/LightData` / `field_feature_category_discover`
- `/FieldIdentity/LightData` / `field_identity_discover`
- `/FieldMembershipCategory/LightData` / `field_membership_category_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Regenerate from the owning service's freshly compiled OpenAPI, refresh dependent schema snapshots, then regenerate consuming clients/DTOs. Do not edit generated C# to restore removed methods. Keep the hand-authored discovery client extensions alongside generated clients: they iterate bounded light pages when a complete name/reference list is needed. Reference projections are partial display/context objects; retrieve a selected complete record before full-replacement writes or engineering calculations. Schema-name normalization must also rewrite discriminator mappings.
