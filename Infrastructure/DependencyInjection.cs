using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Validators;
using Domain.Contracts;

namespace Infrastructure
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Validators
            services.AddScoped<ITeamValidator, TeamValidator>();
            
            // Auction services
            services.AddHostedService<Infrastructure.Services.AuctionFinalizationHostedService>();
            services.AddScoped<Application.Services.IAuctionCommands, Infrastructure.Services.AuctionCommands>();
            
            // Timer data service
            services.AddScoped<Application.Services.IAuctionTimerDataService, Infrastructure.Services.AuctionTimerDataService>();
            
            // New improved services
            services.AddSingleton<Infrastructure.Services.ImprovedAuctionTimerManager>();
            services.AddScoped<Infrastructure.Services.IMagicLinkService, Infrastructure.Services.MagicLinkService>();
            
            // Use the improved timer manager as the implementation of IAuctionTimerManager
            services.AddSingleton<Application.Services.IAuctionTimerManager>(provider => 
                provider.GetRequiredService<Infrastructure.Services.ImprovedAuctionTimerManager>());
            
            return services;
        }
    }
}
