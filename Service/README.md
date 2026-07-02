# Service Project

The Service project hosts the Field microservice (ASP.NET Core, .NET 8). It exposes REST endpoints for managing Field data, field vocabularies, delineation line types, and Field Cartographic Conversion Sets, persists state in SQLite, and publishes an OpenAPI/Swagger UI.

## Purpose

- Serve the REST API for Field entities, field feature categories, field membership categories, field identities, delineation line types, and cartographic conversions.
- Expose an MCP endpoint with tools mirroring the REST API, plus optional MCP hub registration.
- Persist data in a local SQLite database under `../home/Field.db`.
- Expose a merged OpenAPI document and Swagger UI for client generation and testing.
- Orchestrate calls to the external CartographicProjection microservice for conversion calculations.
- Calculate delineation boundary lines from field delineation lines and margins during Field create/update.

## Installation

Prerequisites:
- .NET SDK 8.0+
- Optional: Docker (for containerized runs)

Configuration:
- `CartographicProjectionHostURL`: base URL of the CartographicProjection microservice.
  - Set in `Service/appsettings.Development.json` or environment (e.g., `https://dev.digiwells.no/` for dev).
  - Can also be provided via environment variable at runtime.
- Optional external service configuration is loaded from `../home/Field.Service.json`, or from the path specified by `FIELD_EXTERNAL_CONFIG`.
- In Docker, the image reads optional external configuration from `/home/Field.Service.json`.

External configuration example:

```json
{
  "McpHub": {
    "Enabled": true,
    "HubBaseUrl": "https://mcp-hub.example.com/api",
    "RegistrationEndpoint": "McpMicroservice",
    "RetryIntervalSeconds": 60,
    "PublicBaseUrl": "https://dev.digiwells.no",
    "ServiceName": "Field",
    "InstanceId": "",
    "UnregisterOnShutdown": true
  }
}
```

Build and run (dev):
```bash
# from solution root
 dotnet restore
 dotnet build Field.sln
 dotnet run --project Service
```

Default URLs (from `launchSettings.json`):
- HTTP: `http://localhost:5002/Field/api`
- HTTPS: `https://localhost:5001/Field/api`

The service uses a Path Base of `/Field/api`, so all routes are rooted there.

SQLite storage:
- A writable folder `home` is expected at the solution root; the database file is `home/Field.db`.
- The service creates `home` if missing and manages schema migrations and backups automatically.
- The same shared folder can hold `Field.Service.json` and the generated MCP hub instance id.

## Usage

Swagger UI:
- Navigate to `https://localhost:5001/Field/api/swagger` (or HTTP on 5002) to explore and try the API.
- The UI points to a merged OpenAPI document served at `/Field/api/swagger/merged/swagger.json`.

Endpoints (high level):
- `Field` (controller base path: `/Field/api/Field`)
  - `GET /Field` — list of IDs
  - `GET /Field/MetaInfo` — list of MetaInfo
  - `GET /Field/{id}` — get by ID
  - `GET /Field/HeavyData` — list (full objects)
  - `POST /Field` — create; body is a Field
  - `PUT /Field/{id}` — update; body is a Field
  - `DELETE /Field/{id}` — delete by ID
- `FieldFeatureCategory` (controller base path: `/Field/api/FieldFeatureCategory`)
  - CRUD API for user-managed field feature categories and embedded options.
  - Supports category exclusivity and optional validity periods used by field feature assignments.
- `FieldMembershipCategory` (controller base path: `/Field/api/FieldMembershipCategory`)
  - CRUD API for user-managed field membership categories and embedded options.
  - Supports category exclusivity and optional validity periods used by field membership assignments.
- `FieldIdentity` (controller base path: `/Field/api/FieldIdentity`)
  - CRUD API for symbolic identity definitions, such as Official name, WITSML UID, or External database ID.
- `FieldDelineationLineType` (controller base path: `/Field/api/FieldDelineationLineType`)
  - CRUD API for delineation line types such as lease line, border line, protected area, or no drilling zone.
- `FieldCartographicConversionSet` (controller base path: `/Field/api/FieldCartographicConversionSet`)
  - `GET /FieldCartographicConversionSet` — list of IDs
  - `GET /FieldCartographicConversionSet/MetaInfo` — list of MetaInfo
  - `GET /FieldCartographicConversionSet/{id}` — get by ID
  - `GET /FieldCartographicConversionSet/LightData` — list of Light models
  - `GET /FieldCartographicConversionSet/HeavyData` — list of full models
  - `POST /FieldCartographicConversionSet` — create; triggers conversion via external service
  - `PUT /FieldCartographicConversionSet/{id}` — update; recalculates conversions
  - `DELETE /FieldCartographicConversionSet/{id}` — delete by ID
- `FieldUsageStatistics` (controller base path: `/Field/api/FieldUsageStatistics`)
  - `GET /FieldUsageStatistics` — aggregate per-endpoint counters

## MCP Server

The service exposes a Model Context Protocol endpoint alongside the REST API:

- Streamable HTTP transport: `/Field/api/mcp`
- WebSocket transport: `/Field/api/mcp/ws`

The MCP tool surface mirrors the REST API:

- `ping`
- Field: `field.get_all_ids`, `field.get_all_meta_info`, `field.get_by_id`, `field.get_all`, `field.get_all_light`, `field.create`, `field.update_by_id`, `field.delete_by_id`
- FieldCartographicConversionSet: `field_cartographic_conversion_set.get_all_ids`, `field_cartographic_conversion_set.get_all_meta_info`, `field_cartographic_conversion_set.get_by_id`, `field_cartographic_conversion_set.get_all_by_field_id`, `field_cartographic_conversion_set.get_all_light`, `field_cartographic_conversion_set.get_all`, `field_cartographic_conversion_set.create`, `field_cartographic_conversion_set.update_by_id`, `field_cartographic_conversion_set.delete_by_id`
- FieldFeatureCategory: `field_feature_category.*`
- FieldMembershipCategory: `field_membership_category.*`
- FieldIdentity: `field_identity.*`
- FieldDelineationLineType: `field_delineation_line_type.*`
- Usage statistics: `field_usage_statistics.get`

The `create` and `update_by_id` tools expect the same JSON object body as the corresponding REST endpoints, wrapped in an argument named after the entity, for example `field`, `fieldFeatureCategory`, or `fieldCartographicConversionSet`.

When `McpHub:Enabled` is true, the service registers itself on the configured MCP hub with a fixed service type id, a configured or persisted instance id, and MCP endpoint URLs derived from `PublicBaseUrl`:

- `PublicBaseUrl + "/Field/api/mcp"`
- `PublicBaseUrl` converted to `ws`/`wss` plus `"/Field/api/mcp/ws"`

If `HubBaseUrl` or `PublicBaseUrl` is missing, registration is skipped. If the hub is configured but unreachable, registration is retried every `RetryIntervalSeconds` seconds. On graceful shutdown, the service attempts to unregister its instance when `UnregisterOnShutdown` is true.

Quick examples (curl):
```bash
# create a Field
curl -k -X POST "https://localhost:5001/Field/api/Field" \
  -H "Content-Type: application/json" \
  -d '{
    "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
    "Name": "My Field",
    "Description": "Sample"
  }'

# get by id
curl -k "https://localhost:5001/Field/api/Field/11111111-1111-1111-1111-111111111111"

# list IDs
curl -k "https://localhost:5001/Field/api/Field"
```

Using the generated NSwag client (ModelSharedOut):
```csharp
using NORCE.Drilling.Field.ModelShared;
var baseUrl = "https://localhost:5001/Field/api/";
var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_,_,_,_) => true })
{ BaseAddress = new Uri(baseUrl) };
var client = new Client(baseUrl, http);
await client.PostFieldAsync(new Field { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Name = "My Field" });
```

## Dependencies

Runtime and packages (see `Service/Service.csproj`):
- ASP.NET Core (`Microsoft.NET.Sdk.Web`), .NET 8
- `Microsoft.Data.Sqlite` — SQLite access
- `Swashbuckle.AspNetCore.SwaggerGen` and `SwaggerUI` — Swagger/OpenAPI
- `Microsoft.OpenApi` and `Microsoft.OpenApi.Readers` — merged OpenAPI doc handling
- Project reference to `Model` for domain types

Service composition:
- `Program.cs` sets `UsePathBase("/Field/api")`, configures controllers, CORS (allow any), Swagger UI, and reads `CartographicProjectionHostURL`.
- `Managers/SqlConnectionManager.cs` manages SQLite file lifecycle, schema, and backups.
- `Managers/FieldManager.cs` persists full Field JSON and invokes delineation boundary calculations on create/update.
- `Managers/FieldFeatureCategoryManager.cs`, `FieldMembershipCategoryManager.cs`, `FieldIdentityManager.cs`, and `FieldDelineationLineTypeManager.cs` manage default vocabularies and CRUD storage.
- `Controllers/*Controller.cs` expose REST endpoints and update `UsageStatisticsField` counters in `Model`.
- `APIUtils.cs` configures an HttpClient + NSwag client to call the external CartographicProjection service.

## Integration in the Solution

- Model: Domain types and usage statistics invoked by this service.
- ModelSharedOut: NSwag-generated client and DTOs consumed by tests and potentially the WebApp.
- WebApp: Front-end that calls this service under the same base path (`/Field/api`).
- ServiceTest: NUnit tests that exercise this service over HTTP(S) using the generated client.

## Docker

Build:
```bash
 docker build -t field-service -f Service/Dockerfile .
```

Run (mapping HTTPS and HTTP, provide external service URL):
```bash
 docker run --rm -p 5001:5001 -p 5002:5002 \
  -e ASPNETCORE_URLS="https://+:5001;http://+:5002" \
  -e CartographicProjectionHostURL="https://dev.your-host/" \
  -v %CD%/home:/home \
  field-service
```

Access Swagger at `https://localhost:5001/Field/api/swagger`.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Energy Modelling and Automation
