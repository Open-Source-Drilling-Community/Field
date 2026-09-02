# Field Management WebApp

The WebApp is a Blazor Server front end for the Field microservice. It provides a service overview and hosts the Field and Cluster management packages, field-level vocabulary pages, trajectory and survey-run displays, contextual data pages, and calculator pages.

## Purpose

- Summarize the Field domain, persistence model, available interfaces, and primary workflows on a dedicated Home page.
- Manage Field records through the Field REST API.
- Back up all or selected fields and atomically restore portable version-2 JSON backups, including referenced Field-owned catalog definitions.
- Manage field features, field memberships, field identities, and delineation line types.
- Display trajectories and survey runs for a selected field, including delineation line overlays.
- Configure field-level depth and position references for plotting.
- Provide calculators for cartographic conversion, vertical datum conversion, Earth gravity, and Earth magnetic field.
- Host the reusable EarthCartographicProjection projection-definition pages and the remaining contextual page packages.
- Host the reusable Cluster list/editor as a single contextual-data page.

## Installation

Prerequisites:

- .NET SDK 8.0+

Configuration keys:

- `FieldHostURL`: base URL of the Field service.
- `ClusterHostURL`: base URL of the Cluster service.
- `RigHostURL`: base URL of the Rig service used to resolve Cluster rig references.
- `TrajectoryHostURL`: base URL of the Trajectory service.
- `EarthCartographicProjectionHostURL`: base URL of EarthCartographicProjection.
- `EarthGeodesyHostURL`: base URL of the EarthGeodesy service used by its hosted pages.
- `EarthGravityHostURL`: base URL of the EarthGravity service.
- `EarthMagneticFieldHostURL`: base URL of the EarthMagneticField service.
- `EarthVerticalDatumHostURL`: base URL of the EarthVerticalDatum service.
- `UnitConversionHostURL`: base URL of the UnitConversion service.

Example `WebApp/appsettings.Development.json`:

```json
{
  "DetailedErrors": true,
  "FieldHostURL": "https://dev.digiwells.no/",
  "ClusterHostURL": "https://dev.digiwells.no/",
  "RigHostURL": "https://dev.digiwells.no/",
  "TrajectoryHostURL": "https://dev.digiwells.no/",
  "EarthCartographicProjectionHostURL": "https://dev.digiwells.no/",
  "EarthGeodesyHostURL": "https://dev.digiwells.no/",
  "EarthGravityHostURL": "https://dev.digiwells.no/",
  "EarthMagneticFieldHostURL": "https://dev.digiwells.no/",
  "EarthVerticalDatumHostURL": "https://dev.digiwells.no/",
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

- HTTP: `http://localhost:5012/Field/webapp/Home`
- HTTPS: `https://localhost:5011/Field/webapp/Home`

The app sets `UsePathBase("/Field/webapp")`, so all pages are rooted under that path base.

## Pages

- `Home` (`/Field/webapp/Home`): overview of the Field domain, persistence, primary workflows, and REST, MCP, and web access methods.

Field Management:

- `Field` (`/Field/webapp/Field`): create, edit, delete, and search Field records.
- `Backup and Restore` (`/Field/webapp/FieldBackupRestore`): export all or selected fields and their referenced Field-owned catalog definitions to one versioned JSON file, preview an uploaded backup, and restore it atomically with explicit field-conflict and catalog-mapping policies.
- `Field Features` (`/Field/webapp/FieldFeatures`): manage field feature categories, options, exclusivity, and validity behavior.
- `Field Memberships` (`/Field/webapp/FieldMemberships`): manage membership categories and options such as basin, play, license, operator, or pipeline network.
- `Field Identities` (`/Field/webapp/FieldIdentities`): manage symbolic identity definitions such as Official name, WITSML UID, or External database ID.
- `Delineation Line Types` (`/Field/webapp/FieldDelineationLineTypes`): manage delineation line type names.

Survey Display:

- `Field Trajectories` (`/Field/webapp/FieldTrajectories`): display all trajectories for the selected field in 3D and horizontal projection, with field delineation overlays.
- `Field Survey Runs` (`/Field/webapp/FieldSurveyRuns`): display all survey runs for the selected field in 3D and horizontal projection, with field delineation overlays.
Contextual Data:

- `Clusters` (`/Field/webapp/Cluster`): list, display, create, and edit clusters using the Cluster WebPages package. The package's catalog, backup/restore, display, and statistics routes are intentionally not exposed by the Field webapp.
- `Cartographic Projections` (`/Field/webapp/ProjectionDefinition`): list, display, create, and edit projection definitions using the EarthCartographicProjection WebPages package.
- `Geodetic Datum` (`/Field/webapp/GeodeticDatum`)
- `Spheroid` (`/Field/webapp/Spheroid`)

Calculators:

- `Cartographic Conversion` (`/Field/webapp/FieldCartographicConverter`)
- `Vertical Datum Conversion` (`/Field/webapp/EarthVerticalDatumCalculation`)
- `Earth Gravity Evaluation` (`/Field/webapp/EarthGravityCalculation`)
- `Earth Magnetic Field Evaluation` (`/Field/webapp/EarthMagneticFieldCalculation`)

Monitoring:

- `Usage Statistics` (`/Field/webapp/StatisticsField`): display persistent aggregate request totals in a sortable method, operation, and functional-area table.

## Dependencies

Runtime and packages:

- ASP.NET Core Blazor Server, .NET 8
- MudBlazor
- `OSDC.Drilling.Field.WebPages`
- `OSDC.Drilling.Cluster.WebPages` 1.1.0
- `OSDC.Drilling.EarthCartographicProjection.WebPages`
- `OSDC.Drilling.EarthGeodesy.WebPages`
- `OSDC.Drilling.EarthGravity.WebPages`
- `OSDC.Drilling.EarthMagneticField.WebPages`
- `OSDC.Drilling.EarthVerticalDatum.WebPages`
- `OSDC.DotnetLibraries.General.DataManagement`

Internal structure:

- `Program.cs`: configures Blazor, MudBlazor, path base, host URLs, and service registration.
- `ExternalRazorAssemblies.cs`: exposes Field and external web page assemblies to the Blazor router.
- `ExternalWebPagesServiceCollectionExtensions.cs`: registers API utilities for external web page packages.
- `WebPagesHostConfiguration.cs`: shares host URL configuration across Field and imported web pages.
- `Pages/Home.razor`: provides the Field service and web application overview.
- `Shared/NavMenu.razor`: defines the grouped side menu, including Home and Field batch backup and restore.

## Docker

Build:

```bash
docker build -t digiwells/osdcdrillingfieldwebappclient:stable -f WebApp/Dockerfile .
```

Run:

```bash
docker run --rm -p 5012:8080 \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e FieldHostURL="https://host.docker.internal:5001/" \
  -e ClusterHostURL="https://dev.your-host/" \
  -e RigHostURL="https://dev.your-host/" \
  -e TrajectoryHostURL="https://dev.your-host/" \
  -e EarthCartographicProjectionHostURL="https://dev.your-host/" \
  -e EarthGeodesyHostURL="https://dev.your-host/" \
  -e EarthGravityHostURL="https://dev.your-host/" \
  -e EarthMagneticFieldHostURL="https://dev.your-host/" \
  -e EarthVerticalDatumHostURL="https://dev.your-host/" \
  -e UnitConversionHostURL="https://dev.your-host/" \
  digiwells/osdcdrillingfieldwebappclient:stable
```

The Helm chart is `WebApp/charts/osdcdrillingfieldwebappclient`; its Kubernetes
Service name is `osdcfieldwebappclient`.

Then open `http://localhost:5012/Field/webapp/Home`. TLS is normally
terminated by the Kubernetes ingress in deployed environments.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Research
