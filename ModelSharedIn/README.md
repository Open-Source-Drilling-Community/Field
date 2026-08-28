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
