// FILE #307: TicketBooking.Api/Services/Cms/CmsServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;

namespace TicketBooking.Api.Services.Cms;

public static class CmsServiceCollectionExtensions
{
    /// <summary>
    /// Registers CMS services used by:
    /// - Admin CMS controllers
    /// - Manager CMS controllers
    /// - Public CMS query endpoints
    /// - Future SSR/PublicWeb integration
    /// </summary>
    public static IServiceCollection AddCmsServices(this IServiceCollection services)
    {
        // Core helpers
        services.AddScoped<ICmsSlugService, CmsSlugService>();
        services.AddScoped<ICmsReadingTimeService, CmsReadingTimeService>();
        services.AddScoped<ICmsSeoDefaultsService, CmsSeoDefaultsService>();
        services.AddScoped<ICmsHtmlSanitizer, CmsHtmlSanitizer>();

        // Business services
        services.AddScoped<ICmsRevisionService, CmsRevisionService>();
        services.AddScoped<ICmsRedirectService, CmsRedirectService>();
        services.AddScoped<ICmsPublicQueryService, CmsPublicQueryService>();

        return services;
    }
}


