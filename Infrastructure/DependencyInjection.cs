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
            services.AddScoped<Application.Services.IAuctionCommands, Infrastructure.Services.AuctionCommands>();
            services.AddScoped<Application.Services.IAuctionQueries, Infrastructure.Services.AuctionQueries>();
            
            // Timer data service
            services.AddScoped<Application.Services.IAuctionTimerDataService, Infrastructure.Services.AuctionTimerDataService>();
            
            // Magic link service
            services.AddScoped<Infrastructure.Services.IMagicLinkService, Infrastructure.Services.MagicLinkService>();
            
            return services;
        }
    }
}
