# Model Project

The Model project contains the domain types and supporting utilities for the Field microservice. It defines the core data models exchanged by the Service REST API and used by the managers for persistence and processing.

## Purpose

- Provide strongly typed models for the Field microservice (Field, FieldCartographicConversionSet, FieldCartographicConversionSetLight).
- Centralize lightweight logic and contracts used across Service, WebApp, and tests.
- Track per-endpoint usage metrics via UsageStatisticsField, persisted periodically for diagnostics.

## Key Types

- Field: Represents a field entity with identity (`MetaInfo.ID`), name/description, timestamps, and a reference to a cartographic projection (`CartographicProjectionID`).
- FieldCartographicConversionSet: Input/output payload for cartographic ↔ geodetic conversions related to a given Field; includes `FieldID` and a list of `CartographicCoordinate` items (from ModelSharedIn).
- FieldCartographicConversionSetLight: Lightweight view of conversion set metadata and basic field info.
- UsageStatisticsField: Aggregates per-day counters for REST endpoints (GET/POST/PUT/DELETE) for both Field and FieldCartographicConversionSet resources, with periodic JSON backup to `../home/history.json`.

Namespaces: All types live under `NORCE.Drilling.Field.Model`.

## Dependencies

- .NET: `net8.0`
- NuGet packages:
  - `OSDC.DotnetLibraries.General.DataManagement` (MetaInfo, JSON settings, etc.)
  - `OSDC.DotnetLibraries.General.Common`
  - `OSDC.DotnetLibraries.General.Statistics`
  - `OSDC.DotnetLibraries.Drilling.DrillingProperties`
- Project reference:
  - `ModelSharedIn` — shared input DTOs (e.g., `CartographicCoordinate`, geodetic types) used by conversion models.

See `Model/Model.csproj` for exact versions.

## Integration in the Solution

- Service (ASP.NET Core):
  - Controllers (`Service/Controllers/*Controller.cs`) accept and return these models.
  - Managers (`Service/Managers/*Manager.cs`) serialize/deserialize these models to SQLite and orchestrate calls to external microservices.
  - `UsageStatisticsField` is invoked from controllers to increment usage counters per endpoint.
- ModelSharedOut: NSwag-generated client and DTOs used by tests and possibly the WebApp to call the Service. These are compatible with the models defined here.
- WebApp: Displays and edits entities shaped by these models through the Service API.
- ServiceTest: Uses the NSwag `Client` to exercise endpoints that return/accept the models defined here.

## Usage Examples

Create and POST a Field through the Service API (shape of the payload comes from this project):

```csharp
using NORCE.Drilling.Field.ModelShared; // NSwag client/DTOs

var baseUrl = "https://localhost:5001/Field/api/";
var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_,_,_,_) => true })
{ BaseAddress = new Uri(baseUrl) };
var client = new Client(baseUrl, http);

var fieldId = Guid.NewGuid();
var field = new Field
{
    MetaInfo = new MetaInfo { ID = fieldId },
    Name = "My Field",
    Description = "Sample",
    CreationDate = DateTimeOffset.UtcNow,
    LastModificationDate = DateTimeOffset.UtcNow
};

await client.PostFieldAsync(field);
var fetched = await client.GetFieldByIdAsync(fieldId);
```

Prepare a FieldCartographicConversionSet payload:

```csharp
var fccs = new FieldCartographicConversionSet
{
    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
    Name = "Conversion Set",
    Description = "Sample",
    FieldID = fieldId,
    CartographicCoordinateList = new List<CartographicCoordinate>
    {
        new CartographicCoordinate
        {
            Northing = 1000,
            Easting = 2000,
            VerticalDepth = 50
        }
    }
};

await client.PostFieldCartographicConversionSetAsync(fccs);
```

Record a usage event (inside Service):

```csharp
UsageStatisticsField.Instance.IncrementGetAllFieldIdPerDay();
```

## Notes and Conventions

- Serialization: Types are designed for System.Text.Json. Default constructors exist for JSON compatibility.
- Persistence: Managers serialize full objects into SQLite text columns as JSON, alongside selected scalar columns for querying.
- History backup: `UsageStatisticsField` writes to `../home/history.json` every few minutes. Ensure the `home` directory exists with write permissions in the Service runtime context.

## Building

- Restore and build from the solution root:

```bash
 dotnet build Field.sln
```

The Model project builds as a class library targeting .NET 8 and is referenced by the Service and other projects in this solution.

