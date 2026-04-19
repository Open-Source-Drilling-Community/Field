using Microsoft.Extensions.DependencyInjection;

namespace NORCE.Drilling.Field.WebApp;

public static class ExternalWebPagesServiceCollectionExtensions
{
    public static IServiceCollection AddExternalWebPages(this IServiceCollection services, WebPagesHostConfiguration configuration)
    {
        services.AddSingleton<NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionAPIUtils,
            NORCE.Drilling.CartographicProjection.WebPages.CartographicProjectionAPIUtils>();
        services.AddSingleton<NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumWebPagesConfiguration>(configuration);
        services.AddSingleton<
            NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumAPIUtils,
            NORCE.Drilling.GeodeticDatum.WebPages.GeodeticDatumAPIUtils>();
        return services;
    }
}
