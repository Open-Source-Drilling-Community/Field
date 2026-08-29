# Field

The Field solution provides a microservice (REST API), reusable Razor pages, and a Blazor Server web application to manage Field data, display field trajectories and survey runs, maintain field-level vocabularies and delineation lines, and run contextual calculators. It also includes shared models and generators for OpenAPI-based clients used across the solution.

## Purpose

- Expose CRUD APIs for Field data and managed vocabularies, versioned batch export, plus synchronous stateless field coordinate conversion.
- Expose an MCP endpoint for Field CRUD, portable batch transfer, and stateless conversion, with optional MCP hub registration from the shared `home/` volume.
- Provide a web UI to browse and edit fields, manage field vocabularies, maintain delineation lines, display field-level trajectories and survey runs, and run cartographic, vertical datum, Earth gravity, and Earth magnetic-field calculations.
- Share OpenAPI-generated clients/DTOs to keep contracts consistent across Service, WebApp, and tests.

Field writes validate every reference to the locally owned feature, membership,
identity, and delineation-line-type catalogs. A referenced catalog definition or
option cannot be deleted, and a category update cannot remove an option still in
use. Conflicts identify the affected Field UUIDs. PUT operations are full
replacements and require `expectedModifiedUtc` from the latest representation;
stale updates return HTTP 409 without changing stored data.

## Installation

Prerequisites:
- .NET SDK 8.0+
- Optional: Docker (for containerized runs)

Steps (dev):
- Restore and build the solution:
  - `dotnet restore`
  - `dotnet build Field.sln`
- Generate/refresh the NSwag client and merged OpenAPI (optional during dev):
  - `dotnet run --project ModelSharedOut`
- Run the Service:
  - `dotnet run --project Service`
  - Base path: `https://localhost:5001/Field/api` and `http://localhost:5002/Field/api`
- Run the WebApp:
  - `dotnet run --project WebApp`
  - Base path: `https://localhost:5011/Field/webapp` and `http://localhost:5012/Field/webapp`

Configuration:
- Service reads `EarthCartographicProjectionHostURL` and `EarthGeodesyHostURL` (see `Service/appsettings.*.json`).
- Service can read optional external configuration from `home/Field.Service.json`, or from the path specified by `FIELD_EXTERNAL_CONFIG`.
- WebApp reads `FieldHostURL`, `ClusterHostURL`, `RigHostURL`, `TrajectoryHostURL`, `EarthCartographicProjectionHostURL`, `EarthGeodesyHostURL`, `EarthGravityHostURL`, `EarthMagneticFieldHostURL`, `EarthVerticalDatumHostURL`, and `UnitConversionHostURL`.

Code generation:
- When a pinned EarthCartographicProjection or EarthGeodesy contract changes, run `dotnet run --project ModelSharedIn` and commit its schemas and generated outputs.
- When a Field model or controller contract changes, run `dotnet build Service/Service.csproj -c Debug` to refresh `ModelSharedOut/json-schemas/FieldFullName.json`, then run `dotnet run --project ModelSharedOut` and commit the generated outputs.

## Usage Examples

Swagger UI (Service):
- Local: `https://localhost:5001/Field/api/swagger` (merged schema served at `/Field/api/swagger/merged/swagger.json`)
- Dev example: `https://dev.digiwells.no/Field/api/swagger`

Quick curl (create a Field):
```
curl -k -X POST "https://localhost:5001/Field/api/Field" \
  -H "Content-Type: application/json" \
  -d '{
    "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
    "Name": "My Field",
    "Description": "Sample"
  }'
```

Batch export every field into a versioned JSON document:
```
curl -k -X POST "https://localhost:5001/Field/api/Field/BatchExport" \
  -H "Content-Type: application/json" \
  -d '{ "Scope": "All" }' \
  --output fields.json
```

Use `Scope: "Selected"` with a non-empty, unique `FieldIDs` array to export
specific fields in the requested order. The complete request fails if any UUID
is empty, duplicated, or absent. An `All` export is ordered by UUID for stable
output. The version-2 document identifies itself as
`OSDC.Drilling.Field.BatchExport` and contains complete Field records together
with the referenced Field Feature, Field Membership, Field Identity and
Delineation Line Type definitions. Projection definitions remain external
resources identified by UUID.

Atomically restore that document without overwriting existing fields:
```
curl -k -X POST "https://localhost:5001/Field/api/Field/BatchRestore" \
  -H "Content-Type: application/json" \
  --data-binary @- <<'JSON'
{
  "ConflictPolicy": "FailIfExists",
  "CatalogPolicy": "MapOrCreateMissing",
  "Document": {
    "FormatIdentifier": "OSDC.Drilling.Field.BatchExport",
    "SchemaVersion": 2,
    "ExportedAtUtc": "2026-08-27T12:00:00Z",
    "CatalogDependencies": {
      "FeatureCategories": [],
      "MembershipCategories": [],
      "Identities": [],
      "DelineationLineTypes": []
    },
    "Fields": [
      {
        "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
        "Name": "My Field"
      }
    ]
  }
}
JSON
```

`FailIfExists` rejects the whole batch if any UUID is already stored.
`ReplaceExisting` inserts missing fields and replaces existing fields in the
same transaction. Format, version, UTC timestamp, field identities, duplicate
UUIDs, and projection UUIDs are validated before writing. A storage failure at
any position rolls back every earlier write. `MapExisting` resolves catalog
UUIDs by compatible local UUID or a unique normalized-name match and rejects
missing definitions. `MapOrCreateMissing` additionally creates missing local
definitions and options with server-generated UUIDs. Catalog matching,
reference rewriting, catalog creation and field restoration are atomic.

WebApp (UI):
- Local Field page: `https://localhost:5011/Field/webapp/Field`
- Dev example: `https://dev.digiwells.no/Field/webapp/Field`
- Managed vocabulary pages:
  - `/Field/webapp/FieldFeatures`
  - `/Field/webapp/FieldMemberships`
  - `/Field/webapp/FieldIdentities`
  - `/Field/webapp/FieldDelineationLineTypes`
- Batch backup and restore: `/Field/webapp/FieldBackupRestore`
- Hosted Cluster management: `/Field/webapp/Cluster`
- Hosted Cluster backup and restore: `/Field/webapp/ClusterBackupRestore`

MCP server:
- Streamable HTTP: `/Field/api/mcp`
- WebSocket: `/Field/api/mcp/ws`
- Conversion tools are `field_forward_convert_coordinates` and `field_inverse_convert_coordinates`; requests and results are never persisted.
- Portable transfer tools are `field_batch_export` and `field_batch_restore`. The restore tool is marked destructive and exposes the schema-version-2 catalog mapping/creation policies and atomic transaction contract.
- Usage statistics remain available through REST and are intentionally not exposed as an MCP tool.
- Optional MCP hub registration is configured with `McpHub` in `Field.Service.json`; MCP URLs are derived from `McpHub:PublicBaseUrl`.

# Solution architecture

The solution is composed of:
- **ModelSharedIn**
  - pins the EarthCartographicProjection and EarthGeodesy OpenAPI schemas and generates their C# clients/DTOs
  - *dependencies* = dependency OpenAPI schemas + NSwag
- **Model**
  - defines Field entities, managed vocabularies, batch-transfer contracts, coordinate-conversion contracts, and usage counters
  - *dependencies* = ModelSharedIn + OSDC domain packages
- **Service**
  - defines the proper microservice API
  - exposes CRUD controllers for Field-owned data and synchronous forward/inverse field coordinate conversion
  - exposes MCP CRUD, portable batch transfer, and stateless conversion tools and can publish its MCP endpoint to an MCP hub; usage statistics remain REST-only
  - computes delineation boundary lines during Field create/update
  - *dependencies* = Model
- **ModelSharedOut**
  - generates the Field client/DTOs and merged OpenAPI document used by the WebApp, tests, and Swagger UI
  - *dependencies* = Field, Cluster, and selected dependency OpenAPI schemas + NSwag
- **ModelTest**
  - performs unit tests on the Model (in particular for base computations)
  - *dependencies* = Model
- **ServiceTest**
  - end-to-end client tests against a running Service (the configured default is `https://localhost:5001/`)
  - *dependencies* = ModelShared
- **ServiceUnitTest**
  - in-process tests for controllers, managers, MCP contracts, batch transfer, and conversion orchestration
  - *dependencies* = Service + Model
- **ServiceMcpTest**
  - protocol-level tests against a running Field MCP endpoint
  - *dependencies* = MCP client packages + a running Service
- **WebApp**
  - Blazor Server webapp named `Field Management`
  - hosts Field and Cluster management, vocabulary management, trajectory and survey-run displays, contextual data pages, and calculator pages
  - *dependencies* = WebPages plus reusable Cluster, EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, and EarthMagneticField web page packages
- **WebPages**
  - reusable Razor class library containing the Field web pages
  - includes field management, vocabulary management, delineation editing/import/export, field trajectory display, field survey run display, cartographic conversions, and usage statistics pages
  - *dependencies* = ModelSharedOut + WebAppUtils + DrillingRazorMudComponents
- **home** (auto-generated)
  - data are persisted in the microservice container using the Sqlite database located at *home/Field.db*
  - optional service configuration and the generated MCP hub instance id can also live in this shared folder

## Dependencies

- Core runtime: .NET 8
- Service: ASP.NET Core, `Microsoft.Data.Sqlite`, `Swashbuckle.AspNetCore`, `Microsoft.OpenApi`
- WebApp: Blazor Server, MudBlazor, and reusable Cluster, EarthCartographicProjection, EarthGeodesy, EarthVerticalDatum, EarthGravity, and EarthMagneticField Razor page packages
- WebPages: MudBlazor, `OSDC.DotnetLibraries.Drilling.WebAppUtils`, `OSDC.DotnetLibraries.General.Math`, Plotly.Blazor
- Shared model/codegen: `NSwag.CodeGeneration.CSharp`, `Microsoft.OpenApi.Readers`
- Domain model: OSDC DotnetLibraries (`General.DataManagement` and `DrillingProperties`)

# Security/Confidentiality

Data are persisted as clear text in a SQLite database under the configured `home` directory, normally backed by a Docker volume or Kubernetes persistent volume.
Neither authentication nor authorization have been implemented.
Would you like or need to protect your data, docker containers of the microservice and webapp are available on dockerhub, under the digiwells organization, at:

https://hub.docker.com/?namespace=digiwells

More info on how to run the container and map its database to a folder on your computer, at:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

# Deployment

Microservice is available at:

https://dev.digiwells.no/Field/api/Field

https://app.digiwells.no/Field/api/Field

Web app is available at:

https://dev.digiwells.no/Field/webapp/Field

https://app.digiwells.no/Field/webapp/Field

The OpenApi schema of the microservice is available and testable at:

https://dev.digiwells.no/Field/api/swagger (development server)

https://app.digiwells.no/Field/api/swagger (production server)

The microservice and webapp are deployed as Docker containers using Kubernetes and Helm. The deployment identities are `osdcdrillingfieldservice` and `osdcdrillingfieldwebappclient`; their Kubernetes service names are `osdcfieldservice` and `osdcfieldwebappclient`.

More info at:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

# Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on Digitalization, Drilling Engineering and GeoSteering.

# Contributors

**Eric Cayeux**, *NORCE Research*
