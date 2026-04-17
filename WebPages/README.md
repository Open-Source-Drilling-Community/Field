# NORCE.Drilling.Field.WebPages

`NORCE.Drilling.Field.WebPages` is a Razor class library that packages the `Field`, `FieldEdit`, `CartographicConverter`, and `StatisticsMain` pages together with the page utilities they require.

## Contents

- `Field`
- `FieldEdit`
- `CartographicConverter`
- `StatisticsMain`
- Field page support classes such as API access helpers and unit/reference helpers

## Dependencies

The package depends on:

- `ModelSharedOut`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`

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
    CartographicProjectionHostURL = builder.Configuration["CartographicProjectionHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
});
builder.Services.AddSingleton<IFieldAPIUtils, FieldAPIUtils>();
```

Example routing:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(NORCE.Drilling.Field.WebPages.Field).Assembly }">
```
