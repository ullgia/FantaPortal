using Microsoft.Extensions.DependencyInjection;
using Application.Events;

namespace Application
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Timer manager is now provided by Infrastructure layer (ImprovedAuctionTimerManager)
            // services.AddSingleton<Application.Services.IAuctionTimerManager, Application.Services.AuctionTimerManager>();
            
            // Simple timer service disabled in favor of ImprovedAuctionTimerManager
            // services.AddHostedService<Application.Services.SimpleAuctionTimerService>();
            
            // Domain events
            services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
            
            // Realtime notifications
            services.AddSingleton<Application.Services.IRealtimeNotificationService, Application.Services.NoOpRealtimeNotificationService>();
            
            return services;
        }
    }
}
