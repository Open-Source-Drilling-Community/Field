# OSDC.Drilling.Field.WebPages

This release targets MudBlazor 9.9.0 and the matching OSDC shared web component packages.

`OSDC.Drilling.Field.WebPages` is a Razor class library that packages the Field-specific web pages together with the API, editing, import/export, and plotting utilities they require.

## Contents

- `Field`
- `FieldEdit`
- `FieldBackupRestore`: portable version-2 multi-field JSON backup and atomic restore UI, including referenced Field-owned catalogs and explicit mapping/creation policies
- `FieldFeatures`
- `FieldMemberships`
- `FieldIdentities`
- `FieldDelineationLineTypes`
- `FieldDelineationEditor`
- `FieldTrajectories`
- `FieldSurveyRuns`
- `FieldCartographicConverter`
- `StatisticsField`: persistent aggregate request summaries and sortable endpoint activity
- Field page support classes such as API access helpers, field reference datum helpers, and Plotly-based 2D/3D plotting components.

Field editing supports:

- reference point editing in north/east and latitude/longitude
- reference-aware coordinates and labels for Field, cartographic grid, projection datum, and WGS 84 displays
- field feature, membership, and identity assignments
- delineation line editing, ASCII import, JSON import/export, margin/depth information, and calculated boundary lines

Field trajectory and survey-run displays support:

- a complete, case-insensitive searchable field selector; an empty search lists every available field
- WGS84, Field, and cartographic position references; `Field` uses the selected Field's persisted reference-point north/east offset, while `Cartographic` resolves the selected Field reference point through the Field coordinate-conversion API and applies the resulting projected-grid offset
- automatic fallback to WGS84 when the selected Field does not provide enough reference data for the requested Field or cartographic datum
- 3D and horizontal projection views
- plots remain available when a field has only cluster slots or delineation lines and no survey runs or trajectories
- uncertainty ellipse overlays
- cluster-slot marker overlays in both views, excluding single-well clusters; each cluster is one legend-toggleable trace
- field delineation overlays in both views, with each original line and its calculated boundaries controlled by one legend entry
- WGS84-canonical overlay depths: cluster slots use cluster reference depth, then ground/mud-line depth, then converted 0 MSL; delineations use the average defined field ground/mud-line depth or converted 0 MSL

Ellipse overlays call the Trajectory service's resource-specific SurveyRun or Trajectory endpoint. The service reconstructs authoritative uncertainty ancestry before returning ellipses, so the plots do not silently restart Wolff-de Wardt propagation from a tied or sidetrack resource.

The cartographic converter uses the same complete, case-insensitive partial-name Field autocomplete for both forward and inverse conversion; entering no search text lists every Field that has a projection definition.

## Dependencies

The package depends on:

- `ModelSharedOut`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`
- `Plotly.Blazor`

## Host application requirements

The consuming web app is expected to:

1. Reference this package.
2. Provide an implementation of `IFieldWebPagesConfiguration`.
3. Register that configuration and `IFieldAPIUtils` in dependency injection.
4. Include the library assembly in Blazor routing via `AdditionalAssemblies`.

Example registration:

```csharp
builder.Services.AddSingleton<IFieldWebPagesConfiguration>(new WebPagesHostConfiguration
{
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"] ?? string.Empty,
    EarthCartographicProjectionHostURL = builder.Configuration["EarthCartographicProjectionHostURL"] ?? string.Empty,
    EarthVerticalDatumHostURL = builder.Configuration["EarthVerticalDatumHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
});
builder.Services.AddSingleton<IFieldAPIUtils, FieldAPIUtils>();
```

Example routing:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(OSDC.Drilling.Field.WebPages.Field).Assembly }">
```

## Routes

- `/Field`
- `/FieldBackupRestore`
- `/FieldFeatures`
- `/FieldMemberships`
- `/FieldIdentities`
- `/FieldDelineationLineTypes`
- `/FieldTrajectories`
- `/FieldSurveyRuns`
- `/FieldCartographicConverter`
- `/StatisticsField`

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Research

## Shared resource classification (0.1.0)

The model and WebPages projects now reference
`OSDC.DotnetLibraries.General.ResourceClassification` 0.1.0. DataManagement
2.2.0 continues to own the existing interfaces. Service-specific classification
classes inherit the common implementation while retaining their public names,
JSON properties, nullable references and concrete option lists. Catalogue data,
default UUIDs, database tables, transactions and resource relationships remain
owned by this service.

Reference-integrity validation uses the shared helpers, preserving existing
error codes and unlinked drafts. Editor validity-overlap checks use the common
inclusive-interval rule. This extraction does not add stricter server validation
for IDs or periods; the package also exposes those helpers for a subsequent
audited migration. Persisted records are not rewritten.

Publish ResourceClassification 0.1.0 before clean CI/Docker builds. For local
pre-publication checks, supply the packed package directory as an explicit
NuGet restore source alongside nuget.org; there are no conditional references.
Model classification contract tests verify serialization and typed options.

## Semantic contract conventions

The regenerated shared models follow SemanticCatalogue 0.7.0 descriptions. UI unit selectors remain presentation conversions: wire and stored angular values are radians and linear values are metres. WGS84 ellipsoidal depth is positive downward; display depth references must convert back before saving. Riemannian north/east coordinates are distinct from projected easting/northing. See the Model README for scalar quantity and uncertainty bindings.
