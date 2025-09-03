using Microsoft.Extensions.DependencyInjection;
using Application.Events;
using Application.Events.Handlers;

namespace Application
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Timer services
            services.AddSingleton<Application.Services.IAuctionTimerManager, Application.Services.AuctionTimerManager>();
            services.AddScoped<Application.Services.AuctionTimerService>();
            
            // Domain events
            services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
            
            // Domain event handlers
            services.AddScoped<IDomainEventHandler<BiddingTimerExpired>, BiddingTimerExpiredHandler>();
            
            // Realtime notifications
            services.AddSingleton<Application.Services.IRealtimeNotificationService, Application.Services.NoOpRealtimeNotificationService>();
            
            return services;
        }
    }
}
