# NORCE.Drilling.Field.WebPages

`NORCE.Drilling.Field.WebPages` is a Razor class library that packages the Field-specific web pages together with the API and plotting utilities they require.

## Contents

- `Field`
- `FieldEdit`
- `FieldTrajectories`
- `FieldSurveyRuns`
- `FieldCartographicConverter`
- `StatisticsField`
- Field page support classes such as API access helpers, field reference datum helpers, and Plotly-based 2D/3D plotting components

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
    CartographicProjectionHostURL = builder.Configuration["CartographicProjectionHostURL"] ?? string.Empty,
    GeodeticDatumHostURL = builder.Configuration["GeodeticDatumHostURL"] ?? string.Empty,
    VerticalDatumHostURL = builder.Configuration["VerticalDatumHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
});
builder.Services.AddSingleton<IFieldAPIUtils, FieldAPIUtils>();
```

Example routing:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(NORCE.Drilling.Field.WebPages.Field).Assembly }">
```

## Routes

- `/Field`
- `/FieldTrajectories`
- `/FieldSurveyRuns`
- `/FieldCartographicConverter`
- `/StatisticsField`

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the center for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Energy Modelling and Automation
