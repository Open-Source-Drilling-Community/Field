# ModelSharedOut Project

ModelSharedOut generates and maintains the OpenAPI client + DTOs for the Field microservice, and publishes a merged OpenAPI document used by the Service Swagger UI. It follows the distributed shared model approach, keeping client contracts versioned alongside the solution.

## Purpose

- Merge OpenAPI schemas from this solution into a single document (`FieldMergedModel.json`).
- Generate a strongly-typed C# client and DTOs (`FieldMergedModel.cs`) in namespace `NORCE.Drilling.Field.ModelShared` for consumers like WebApp and ServiceTest.
- Copy the merged OpenAPI document into the Service so it can be served by Swagger UI.

## How It Works

- `json-schemas/` contains input OpenAPI docs (e.g., `FieldFullName.json`, `CartographicProjectionMergedModel.json`).
- `Program.cs`:
  - Reads the JSON inputs, merges Paths and Schemas, normalizes schema names to short names.
  - Writes the merged OpenAPI to `Service/wwwroot/json-schema/FieldMergedModel.json`.
  - Uses NSwag to generate `ModelSharedOut/FieldMergedModel.cs` client + DTOs in `NORCE.Drilling.Field.ModelShared`.

## Generate Client and OpenAPI

Prerequisites:
- .NET SDK 8.0+

From the solution root:
```bash
# regenerate merged OpenAPI + client
 dotnet run --project ModelSharedOut
```
Outputs:
- `Service/wwwroot/json-schema/FieldMergedModel.json` (served by Service Swagger middleware)
- `ModelSharedOut/FieldMergedModel.cs` (referenced by WebApp and ServiceTest)

Note: In Debug builds, `Service/Service.csproj` also invokes `dotnet swagger tofile` to emit `ModelSharedOut/json-schemas/FieldFullName.json`, which feeds into the merge.

## Usage Examples

Create the NSwag client and call the Service:
```csharp
using NORCE.Drilling.Field.ModelShared;

var baseUrl = "https://localhost:5001/Field/api/";
var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_,_,_,_) => true };
using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
var client = new Client(baseUrl, http);

// Create a Field
var fieldId = Guid.NewGuid();
await client.PostFieldAsync(new Field { MetaInfo = new MetaInfo { ID = fieldId }, Name = "My Field" });

// Read lists and single item
var ids = await client.GetAllFieldIdAsync();
var field = await client.GetFieldByIdAsync(fieldId);
```

The generated DTOs include types like `Field`, `MetaInfo`, `FieldCartographicConversionSet`, etc., aligned with the Service controllers.

## Dependencies

NuGet packages (see `ModelSharedOut.csproj`):
- `Microsoft.OpenApi.Readers` — read and parse OpenAPI inputs
- `NSwag.CodeGeneration.CSharp` — generate C# client and DTOs

## Integration in the Solution

- Service: Serves `wwwroot/json-schema/FieldMergedModel.json` through Swagger UI (`/Field/api/swagger/merged/swagger.json`).
- WebApp: References `ModelSharedOut` to call the Service via the generated `Client` and use DTOs.
- ServiceTest: Uses the same client/DTOs to perform end-to-end API tests.
- Model: Compatible with the DTO shapes (e.g., `MetaInfo`) used at the boundaries.

## Tips

- Keep `json-schemas/` up to date (e.g., after changing Service controllers or external dependencies) and rerun the generator.
- The tool normalizes schema names to avoid verbose type identifiers; avoid name collisions across inputs.
- The merged OpenAPI is adjusted to OpenAPI 3.0.3 for tooling compatibility.

