# Field

The Field solution provides a microservice (REST API), reusable Razor pages, and a Blazor Server web application to manage Field data, display field trajectories and survey runs, maintain field-level vocabularies and delineation lines, and run contextual calculators. It also includes shared models and generators for OpenAPI-based clients used across the solution.

## Purpose

- Expose a REST API to create, read, update, and delete Field data, managed field feature categories, field memberships, field identities, delineation line types, and cartographic conversion sets associated with a Field.
- Provide a web UI to browse and edit fields, manage field vocabularies, maintain delineation lines, display field-level trajectories and survey runs, and run cartographic and vertical datum conversions.
- Share OpenAPI-generated clients/DTOs to keep contracts consistent across Service, WebApp, and tests.

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
- Service reads `CartographicProjectionHostURL` (see `Service/appsettings.*.json`).
- WebApp reads `FieldHostURL`, `ClusterHostURL`, `TrajectoryHostURL`, `CartographicProjectionHostURL`, `GeodeticDatumHostURL`, `VerticalDatumHostURL`, and `UnitConversionHostURL` (see `WebApp/appsettings.*.json`).

Code generation:
- When model or controller contracts change, regenerate DTOs in this order:
  - `dotnet run --project ModelSharedIn`
  - `dotnet build Service\Service.csproj` to refresh `ModelSharedOut/json-schemas/FieldFullName.json`
  - `dotnet run --project ModelSharedOut`

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

WebApp (UI):
- Local Field page: `https://localhost:5011/Field/webapp/Field`
- Dev example: `https://dev.digiwells.no/Field/webapp/Field`
- Managed vocabulary pages:
  - `/Field/webapp/FieldFeatures`
  - `/Field/webapp/FieldMemberships`
  - `/Field/webapp/FieldIdentities`
  - `/Field/webapp/FieldDelineationLineTypes`

# Solution architecture

The solution is composed of:
- **ModelSharedIn**
  - contains C# auto-generated classes of Model dependencies
  - these dependencies are stored as json files (following the OpenApi standard) and C# classes are generated on execution of the program
  - *dependencies* = some external microservices (OpenApi schemas in json format)
- **Model**
  - defines the main classes and methods to run the microservice
  - *dependencies* = BaseModels
- **Service**
  - defines the proper microservice API
  - exposes CRUD controllers for fields, cartographic conversion sets, field feature categories, field membership categories, field identities, and delineation line types
  - computes delineation boundary lines during Field create/update
  - *dependencies* = Model
- **ModelSharedOut**
  - contains C# auto-generated classes for microservice clients dependencies
  - these dependencies are stored as json files (following the OpenAPI standard) and C# classes are generated on execution of the program
  - these dependencies include the OpenApi schema of the microservice itself as well as other dependencies that may be useful to run the microservice
  - *dependencies* = Field.json + some external microservices (OpenApi schemas in json format)
- **ModelTest**
  - performs unit tests on the Model (in particular for base computations)
  - *dependencies* = Model
- **ServiceTest**
  - microservice client that performs unit tests on the microservice (by default, an instance of the microservice must be running on http port 8080 to run tests)
  - *dependencies* = ModelShared
- **WebApp**
  - Blazor Server webapp named `Field Management`
  - hosts field management, vocabulary management, field trajectory and survey run displays, contextual data pages, and calculator pages
  - *dependencies* = WebPages + reusable CartographicProjection, GeodeticDatum, and VerticalDatum web page packages
- **WebPages**
  - reusable Razor class library containing the Field web pages
  - includes field management, vocabulary management, delineation editing/import/export, field trajectory display, field survey run display, cartographic conversions, and usage statistics pages
  - *dependencies* = ModelSharedOut + WebAppUtils + DrillingRazorMudComponents
- **home** (auto-generated)
  - data are persisted in the microservice container using the Sqlite database located at *home/Field.db*

## Dependencies

- Core runtime: .NET 8
- Service: ASP.NET Core, `Microsoft.Data.Sqlite`, `Swashbuckle.AspNetCore`, `Microsoft.OpenApi`
- WebApp: Blazor Server, MudBlazor, external Razor page packages for cartographic projection, geodetic datum, and vertical datum
- WebPages: MudBlazor, `OSDC.UnitConversion.DrillingRazorMudComponents`, Plotly.Blazor
- Shared model/codegen: `NSwag.CodeGeneration.CSharp`, `Microsoft.OpenApi.Readers`
- Domain model: OSDC DotnetLibraries (General.DataManagement, General.Common, General.Statistics, DrillingProperties)

# Security/Confidentiality

Data are persisted as clear text in a unique Sqlite database hosted in the docker container.
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

The microservice and webapp are deployed as Docker containers using Kubernetes and Helm. More info at:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

# Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the cent for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on Digitalization, Drilling Engineering and GeoSteering. 

# Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*
