# ModelSharedIn Project

ModelSharedIn manages the shared input data model that the Field solution depends on. It bundles external OpenAPI documents into a single merged schema and generates a C# client + DTOs that are used across the solution.

## Purpose

- Collect and version the OpenAPI schemas of dependent microservices (as JSON files in `json-schemas/`).
- Merge those schemas into a single OpenAPI document (`MergedModel.json`).
- Generate a strongly-typed C# client and DTOs (`MergedModel.cs`) under the namespace `NORCE.Drilling.Field.ModelShared` for consumption by other projects.

This implements a “distributed shared model” pattern: each microservice owns the subset of external types it needs, generated directly from the source OpenAPI specs.

## How It Works

- The console program (`Program.cs`) scans `json-schemas/*.json` for dependency schemas, merges paths and schemas, normalizes schema names (short type names), and writes:
  - `MergedModel.json`: merged OpenAPI (for inspection/verification)
  - `MergedModel.cs`: NSwag-generated C# client and DTOs
- The generated types live in `NORCE.Drilling.Field.ModelShared`, aligning with consumers like `Service`, `WebApp`, and `ServiceTest`.

## Installation and Generation

Prerequisites:
- .NET SDK 8.0+

Steps:
1. Place dependency OpenAPI JSON files into `ModelSharedIn/json-schemas/`.
   - Typically fetched from dependency Swagger endpoints, e.g. `https://host/SomeService/api/swagger/merged/swagger.json` or `.../swagger/v1/swagger.json`.
2. Generate the merged model and client:
   - From the solution root:
     ```bash
     dotnet run --project ModelSharedIn
     ```
   - Or from the project directory:
     ```bash
     dotnet run
     ```
3. Commit the updated `MergedModel.cs` (and optionally `MergedModel.json`).

Outputs:
- `ModelSharedIn/MergedModel.cs`
- `ModelSharedIn/MergedModel.json`

## Usage Examples

Using the generated DTOs in code:
```csharp
using NORCE.Drilling.Field.ModelShared;

var coord = new CartographicCoordinate
{
    Northing = 1000,
    Easting = 2000,
    VerticalDepth = 50
};

var meta = new MetaInfo { ID = Guid.NewGuid() };
```

Using the generated client against a base URL:
```csharp
using NORCE.Drilling.Field.ModelShared;

var baseUrl = "https://localhost:5001/Field/api/";
using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_,_,_,_) => true })
{ BaseAddress = new Uri(baseUrl) };
var client = new Client(baseUrl, http);

var ids = await client.GetAllFieldIdAsync();
```

Note: The available operations depend on the OpenAPI docs you included in `json-schemas/`.

## Dependencies

NuGet packages (see `ModelSharedIn.csproj`):
- `Microsoft.OpenApi.Readers` — parse OpenAPI documents
- `NSwag.CodeGeneration.CSharp` — generate C# client and DTOs

## Integration in the Solution

- Model: References these generated DTOs (e.g., `CartographicCoordinate`, `GeodeticCoordinate`, `MetaInfo`) in domain types like `FieldCartographicConversionSet`.
- Service and WebApp: Use types under `NORCE.Drilling.Field.ModelShared` to communicate and to construct payloads consistent with the OpenAPI contracts.
- ServiceTest: Uses the NSwag `Client` and DTOs from the same namespace to perform end-to-end API tests.

## Tips

- Keep `json-schemas/` updated when dependencies evolve. Re-run the generator and review diffs in `MergedModel.cs`.
- The program normalizes type names to short names to avoid verbose schema identifiers; ensure names across dependencies do not collide.
- The generator forces OpenAPI `3.0.3` in output for better tooling compatibility.

