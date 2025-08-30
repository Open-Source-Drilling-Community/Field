# WebApp Project

The WebApp is a Blazor Server front end for the Field microservice. It provides pages to browse, create, edit, and delete Field entities, run cartographic conversions, and visualize usage statistics. The UI uses MudBlazor components and calls the Service REST API via an NSwag-generated client.

## Purpose

- Offer a user-friendly interface on top of the Field REST API.
- Orchestrate interactions with related microservices for cartographic projections and unit conversions.
- Demonstrate end-to-end usage of the Service with live data and conversions.

## Installation

Prerequisites:
- .NET SDK 8.0+

Configuration (appsettings):
- `FieldHostURL`: base URL of the Field Service (e.g., `https://localhost:5001/`).
- `CartographicProjectionHostURL`: base URL of the CartographicProjection service.
- `UnitConversionHostURL`: base URL of the UnitConversion service.

Example `WebApp/appsettings.Development.json`:
```
{
  "DetailedErrors": true,
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "FieldHostURL": "https://localhost:5001/",
  "CartographicProjectionHostURL": "https://dev.digiwells.no/",
  "UnitConversionHostURL": "https://dev.digiwells.no/"
}
```

Build and run (dev):
```
dotnet restore
dotnet build Field.sln
dotnet run --project WebApp
```

Default URLs (from `Properties/launchSettings.json`):
- HTTP: `http://localhost:5012/Field/webapp/FieldMain`
- HTTPS: `https://localhost:5011/Field/webapp/FieldMain`

Note: The app sets `UsePathBase("/Field/webapp")`, so all pages are rooted under that path base.

## Usage

Main pages:
- `FieldMain` (`/Field/webapp/FieldMain`): manage the list of Fields (add, select, delete, search, open editor).
- `CartographicConverter` (`/Field/webapp/CartographicConverter`): run cartographic ↔ geodetic conversions.
- `Statistics` (`/Field/webapp/Statistics`): view per-endpoint usage counters.

Common actions:
- Add Field: From Field page, click “Add” to create a new Field; the app posts to the Service and opens the editor.
- Edit Field: Select a Field to open `FieldEdit`, update name/description/projection, then save (PUT to Service).
- Delete Field: Select rows and click “Delete” (DELETE to Service).

Client calls:
- The app constructs HttpClient base addresses using the values above and calls the Service through the NSwag `Client` in `ModelSharedOut`.
- Dev-only: SSL validation is bypassed in `APIUtils` to simplify local testing.

## Dependencies

Runtime and packages (see `WebApp/WebApp.csproj`):
- ASP.NET Core (Server-side Blazor), .NET 8
- `MudBlazor` UI via `MudBlazor.Services`
- `OSDC.UnitConversion.DrillingRazorMudComponents` for unit/reference selection
- `OSDC.DotnetLibraries.General.DataManagement`
- Project reference to `ModelSharedOut` for the NSwag client and DTOs

Internal structure:
- `Program.cs`: adds Razor Pages + Blazor, Mud services, path base (`/Field/webapp`), loads host URLs from configuration.
- `Shared/APIUtils.cs`: creates HttpClient instances and NSwag clients to call Field and CartographicProjection services.
- `Shared/DataUtils.cs`: UI labels and unit/reference parameters.
- `Pages/*`: `FieldMain.razor`, `FieldEdit.razor`, `CartographicConverter.razor`, `StatisticsMain.razor`.

## Integration in the Solution

- Service: Primary backend the WebApp calls at `/Field/api` (Field controllers).
- ModelSharedOut: NSwag-generated client used by the WebApp to consume the Service.
- CartographicProjection service: Accessed for projections and conversions.
- UnitConversion service: Provides unit and reference system settings via Razor components.
- ServiceTest: Tests can run independently; the WebApp is not required for API tests but exercises the same endpoints interactively.

## Docker

Build:
```
docker build -t field-webapp ./WebApp
```

Run (map ports and configure backend URLs):
```
docker run --rm -p 5011:5011 -p 5012:5012 \
  -e ASPNETCORE_URLS="https://+:5011;http://+:5012" \
  -e FieldHostURL="https://host.docker.internal:5001/" \
  -e CartographicProjectionHostURL="https://dev.your-host/" \
  -e UnitConversionHostURL="https://dev.your-host/" \
  field-webapp
```

Then open `https://localhost:5011/Field/webapp/FieldMain`.

---

Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020–2028)](https://www.digiwells.no/).

Contributors

- Eric Cayeux, NORCE Energy Modelling and Automation
- Gilles Pelfrene, NORCE Energy Modelling and Automation
- Andrew Holsaeter, NORCE Energy Modelling and Automation
