using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Validators;
using Domain.Contracts;
using Infrastructure.Interceptors;
using Infrastructure.Services;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database services
            services.AddDatabaseServices(configuration);
            
            // Interceptors
            services.AddScoped<DomainEventInterceptor>();
            
            // Validators
            services.AddScoped<ITeamValidator, TeamValidator>();
            
            // Auction services
            services.AddScoped<Application.Services.IAuctionCommands, Infrastructure.Services.AuctionCommands>();
            services.AddScoped<Application.Services.IAuctionQueries, Infrastructure.Services.AuctionQueries>();
            services.AddScoped<Application.Services.ILeagueQueries, Infrastructure.Services.LeagueQueries>();
            services.AddScoped<Application.Services.ILeagueCommands, Infrastructure.Services.LeagueCommands>();
            
            // Timer data service
            services.AddScoped<Application.Services.IAuctionTimerDataService, Infrastructure.Services.AuctionTimerDataService>();
            
            // Magic link service
            services.AddScoped<Infrastructure.Services.IMagicLinkService, Infrastructure.Services.MagicLinkService>();
            
            // Authentication services
            services.AddAuthenticationServices();
            
            return services;
        }
    }
}
