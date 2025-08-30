# Service Project

The Service project hosts the Field microservice (ASP.NET Core, .NET 8). It exposes REST endpoints for managing Field data and Field Cartographic Conversion Sets, persists state in SQLite, and publishes an OpenAPI/Swagger UI.

## Purpose

- Serve the REST API for Field entities and cartographic conversions.
- Persist data in a local SQLite database under `../home/Field.db`.
- Expose a merged OpenAPI document and Swagger UI for client generation and testing.
- Orchestrate calls to the external CartographicProjection microservice for conversion calculations.

## Installation

Prerequisites:
- .NET SDK 8.0+
- Optional: Docker (for containerized runs)

Configuration:
- `CartographicProjectionHostURL`: base URL of the CartographicProjection microservice.
  - Set in `Service/appsettings.Development.json` or environment (e.g., `https://dev.digiwells.no/` for dev).
  - Can also be provided via environment variable at runtime.

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
 docker build -t field-service ./Service 
```

Run (mapping HTTPS and HTTP, provide external service URL):
```bash
 docker run --rm -p 5001:5001 -p 5002:5002 \
  -e ASPNETCORE_URLS="https://+:5001;http://+:5002" \
  -e CartographicProjectionHostURL="https://dev.your-host/" \
  field-service
```

Access Swagger at `https://localhost:5001/Field/api/swagger`.

