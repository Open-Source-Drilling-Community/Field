# ModelSharedIn Project

ModelSharedIn pins the external API contracts consumed by the Field service. It
merges dependency OpenAPI documents and generates the C# clients and DTOs used
to call those services.

## Inputs and outputs

Pinned inputs in `json-schemas/`:

- `EarthCartographicProjectionModel.json`
- `EarthGeodesyModel.json`

Generated, version-controlled outputs:

- `MergedModel.json`: merged OpenAPI 3.0.3 document
- `MergedModel.cs`: NSwag client and DTO source in
  `OSDC.Drilling.Field.ModelShared`

This is the distributed shared model pattern: each microservice versions the
specific external contracts that it consumes instead of sharing runtime model
assemblies with dependent services.

## Regeneration

From the solution root:

```bash
dotnet run --project ModelSharedIn
```

Run this generator whenever either pinned dependency contract changes. Review
and commit both generated outputs and the updated source schemas. Changes that
only affect Field-owned models or controllers do not require ModelSharedIn to
be regenerated.

For a complete dependency and Field contract refresh, use:

```bash
dotnet run --project ModelSharedIn
dotnet build Service/Service.csproj -c Debug
dotnet run --project ModelSharedOut
```

## Use in the solution

- `Model` references this project so the Service can use the pinned
  EarthCartographicProjection and EarthGeodesy clients and DTOs.
- The Service uses those clients for stateless field coordinate conversion and
  live datum-path resolution.
- The WebApp calls the Field API through the separately generated
  `ModelSharedOut` client.

The available generated client methods are determined by the OpenAPI paths in
the two pinned input documents. Do not edit `MergedModel.cs` manually.

## Dependencies

- `Microsoft.OpenApi.Readers`: parses OpenAPI documents
- `NSwag.CodeGeneration.CSharp`: generates the C# clients and DTOs

The generator normalizes schema names to short names. Review generation errors
and diffs for collisions whenever a dependency introduces or renames a schema.

## Contributors

- Eric Cayeux, NORCE Research

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
