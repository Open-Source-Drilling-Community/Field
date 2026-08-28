# ModelSharedOut Project

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
