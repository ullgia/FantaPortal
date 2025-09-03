using Microsoft.Extensions.DependencyInjection;
using Application.Events;

namespace Application
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<Application.Services.IAuctionTimerManager, Application.Services.AuctionTimerManager>();
            services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
            services.AddHostedService<Application.Services.AuctionTimerHostedService>();
            services.AddSingleton<Application.Services.IRealtimeNotificationService, Application.Services.NoOpRealtimeNotificationService>();
            return services;
        }
    }
}
