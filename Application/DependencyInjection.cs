using Microsoft.Extensions.DependencyInjection;
using Application.Events;
using Application.Events.Handlers;
using Domain.Events;

namespace Application
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {

            // Timer calculation strategies (domain services)
            services.AddSingleton<Domain.Services.ITimerCalculationService, Domain.Services.AdaptiveTimerCalculationService>();
            services.AddSingleton<Domain.Services.ITimerCalculationServiceFactory, Domain.Services.TimerCalculationServiceFactory>();
            
            // Domain events
            services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
            
            // Domain event handlers
            services.AddScoped<IDomainEventHandler<BiddingTimerExpired>, BiddingTimerExpiredHandler>();
            services.AddScoped<IDomainEventHandler<BiddingReadyCompleted>, BiddingReadyCompletedHandler>();
            services.AddScoped<IDomainEventHandler<PlayerAssigned>, PlayerAssignedHandler>();
            
            return services;
        }
    }
}
