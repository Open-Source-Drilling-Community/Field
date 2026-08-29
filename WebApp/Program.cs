using MudBlazor;
using MudBlazor.Services;
using OSDC.Drilling.Field.WebApp;
using OSDC.Drilling.Field.WebPages;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

var builder = WebApplication.CreateBuilder(args);

WebPagesHostConfiguration webPagesConfiguration = new()
{
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"] ?? string.Empty,
    EarthCartographicProjectionHostURL = builder.Configuration["EarthCartographicProjectionHostURL"] ?? string.Empty,
    EarthGeodesyHostURL = builder.Configuration["EarthGeodesyHostURL"] ?? string.Empty,
    EarthGravityHostURL = builder.Configuration["EarthGravityHostURL"] ?? string.Empty,
    EarthMagneticFieldHostURL = builder.Configuration["EarthMagneticFieldHostURL"] ?? string.Empty,
    EarthVerticalDatumHostURL = builder.Configuration["EarthVerticalDatumHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddSingleton<IFieldWebPagesConfiguration>(webPagesConfiguration);
builder.Services.AddSingleton<IFieldAPIUtils, FieldAPIUtils>();
builder.Services.AddHostedService<ProjectionDefinitionCatalogWarmupService>();
builder.Services.AddExternalWebPages(webPagesConfiguration);

var app = builder.Build();

app.UseForwardedHeaders();
var basePath = "/field/webapp";
app.UsePathBase(basePath);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

