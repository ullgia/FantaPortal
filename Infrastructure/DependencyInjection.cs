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
            services.AddHostedService<Infrastructure.Services.AuctionFinalizationHostedService>();
            services.AddScoped<Application.Services.IAuctionCommands, Infrastructure.Services.AuctionCommands>();
            return services;
        }
    }
}
