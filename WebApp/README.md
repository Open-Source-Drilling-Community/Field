# Field Management WebApp

The WebApp is a Blazor Server front end for the Field microservice. It hosts the Field management page, field-level trajectory and survey run displays, contextual data pages, and calculator pages.

## Purpose

- Manage Field records through the Field REST API.
- Display trajectories and survey runs for a selected field.
- Configure field-level depth and position references for plotting.
- Provide calculators for cartographic conversions and single vertical datum conversion.
- Reuse contextual data pages from the CartographicProjection, GeodeticDatum, and VerticalDatum web page packages.

## Installation

Prerequisites:

- .NET SDK 8.0+

Configuration keys:

- `FieldHostURL`: base URL of the Field service.
- `ClusterHostURL`: base URL of the Cluster service.
- `TrajectoryHostURL`: base URL of the Trajectory service.
- `CartographicProjectionHostURL`: base URL of the CartographicProjection service.
- `GeodeticDatumHostURL`: base URL of the GeodeticDatum service.
- `VerticalDatumHostURL`: base URL of the VerticalDatum service.
- `UnitConversionHostURL`: base URL of the UnitConversion service.

Example `WebApp/appsettings.Development.json`:

```json
{
  "DetailedErrors": true,
  "FieldHostURL": "https://dev.digiwells.no/",
  "ClusterHostURL": "https://dev.digiwells.no/",
  "TrajectoryHostURL": "https://dev.digiwells.no/",
  "CartographicProjectionHostURL": "https://dev.digiwells.no/",
  "GeodeticDatumHostURL": "https://dev.digiwells.no/",
  "VerticalDatumHostURL": "https://app.digiwells.no/",
  "UnitConversionHostURL": "https://dev.digiwells.no/"
}
```

Build and run from the solution root:

```bash
dotnet restore
dotnet build Field.sln
dotnet run --project WebApp
```

Default URLs:

- HTTP: `http://localhost:5012/Field/webapp/Field`
- HTTPS: `https://localhost:5011/Field/webapp/Field`

The app sets `UsePathBase("/Field/webapp")`, so all pages are rooted under that path base.

## Pages

Field Management:

- `Field` (`/Field/webapp/Field`): create, edit, delete, and search Field records.

Survey Display:

- `Field Trajectories` (`/Field/webapp/FieldTrajectories`): display all trajectories for the selected field in 3D and horizontal projection.
- `Field Survey Runs` (`/Field/webapp/FieldSurveyRuns`): display all survey runs for the selected field in 3D and horizontal projection.

Contextual Data:

- `Cartographic Projections` (`/Field/webapp/CartographicProjection`)
- `Geodetic Datum` (`/Field/webapp/GeodeticDatum`)
- `Spheroid` (`/Field/webapp/Spheroid`)

Calculators:

- `Cartographic Conversions` (`/Field/webapp/FieldCartographicConverter`)
- `Vertical Datum Single Conversion` (`/Field/webapp/VerticalDatumConversion`)

## Dependencies

Runtime and packages:

- ASP.NET Core Blazor Server, .NET 8
- MudBlazor
- `NORCE.Drilling.Field.WebPages`
- `NORCE.Drilling.CartographicProjection.WebPages`
- `NORCE.Drilling.GeodeticDatum.WebPages`
- `NORCE.Drilling.VerticalDatum.WebPage`
- `OSDC.DotnetLibraries.General.DataManagement`

Internal structure:

- `Program.cs`: configures Blazor, MudBlazor, path base, host URLs, and service registration.
- `ExternalRazorAssemblies.cs`: exposes Field and external web page assemblies to the Blazor router.
- `ExternalWebPagesServiceCollectionExtensions.cs`: registers API utilities for external web page packages.
- `WebPagesHostConfiguration.cs`: shares host URL configuration across Field and imported web pages.
- `Shared/NavMenu.razor`: defines the grouped side menu.

## Docker

Build:

```bash
docker build -t field-webapp ./WebApp
```

Run:

```bash
docker run --rm -p 5011:5011 -p 5012:5012 \
  -e ASPNETCORE_URLS="https://+:5011;http://+:5012" \
  -e FieldHostURL="https://host.docker.internal:5001/" \
  -e ClusterHostURL="https://dev.your-host/" \
  -e TrajectoryHostURL="https://dev.your-host/" \
  -e CartographicProjectionHostURL="https://dev.your-host/" \
  -e GeodeticDatumHostURL="https://dev.your-host/" \
  -e VerticalDatumHostURL="https://app.digiwells.no/" \
  -e UnitConversionHostURL="https://dev.your-host/" \
  field-webapp
```

Then open `https://localhost:5011/Field/webapp/Field`.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Energy Modelling and Automation
