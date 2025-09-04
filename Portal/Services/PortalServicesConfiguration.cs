using Microsoft.Extensions.DependencyInjection;
using Portal.Services;
using Portal.Auth;
using Radzen;

namespace Portal.Services;

public static class PortalServicesConfiguration
{
    /// <summary>
    /// Configura tutti i servizi specifici del Portal
    /// </summary>
    public static IServiceCollection AddPortalServices(this IServiceCollection services)
    {
        // SignalR services
        services.AddSignalR();
        services.AddSingleton<Application.Services.IRealtimeNotificationService, SignalRRealtimeNotificationService>();
        services.AddScoped<Portal.Services.IRealtimeNotificationService, Portal.Services.SignalRNotificationService>();
        services.AddScoped<Portal.Services.AuctionHubClient>();
        services.AddScoped<Portal.Services.AuctionRealtimeStore>();

        // Radzen components
        services.AddRadzenComponents();

        // Magic link validator (replace with real implementation later)
        services.AddScoped<IMagicLinkValidator, DatabaseMagicLinkValidator>();
        services.AddScoped<Infrastructure.Services.IMagicLinkService, Infrastructure.Services.MagicLinkService>();
    services.AddScoped<IMagicGrantAccessor, MagicGrantAccessor>();
        
        // HTTP context accessor
        services.AddHttpContextAccessor();

        return services;
    }
}
