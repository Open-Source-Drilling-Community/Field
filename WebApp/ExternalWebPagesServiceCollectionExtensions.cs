using Microsoft.Extensions.DependencyInjection;

namespace OSDC.Drilling.Field.WebApp;

public static class ExternalWebPagesServiceCollectionExtensions
{
    public static IServiceCollection AddExternalWebPages(this IServiceCollection services, WebPagesHostConfiguration configuration)
    {
        services.AddSingleton<
            OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionApi,
            OSDC.Drilling.EarthCartographicProjection.WebPages.EarthCartographicProjectionApi>();
        services.AddSingleton<OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyAPIUtils,
            OSDC.Drilling.EarthGeodesy.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthGravity.WebPages.IEarthGravityAPIUtils,
            OSDC.Drilling.EarthGravity.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldAPIUtils,
            OSDC.Drilling.EarthMagneticField.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumAPIUtils,
            OSDC.Drilling.EarthVerticalDatum.WebPages.APIUtils>();
        return services;
    }
}
