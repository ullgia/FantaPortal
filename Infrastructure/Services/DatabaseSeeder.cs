using Domain.Entities;
using Infrastructure.Peristance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public static class DatabaseSeeder
{
    private const string MasterRoleName = "Master";
    private const string LeaguePlayerRoleName = "LeaguePlayer";
    private const string LeagueGuestRoleName = "LeagueGuest";
    private const string DefaultUserEmail = "admin@fantaasta.com";
    private const string DefaultUserPassword = "Admin123!";

    /// <summary>
    /// Esegue il seeding del database con dati di base
    /// </summary>
    public static async Task SeedDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Crea tutti i ruoli necessari
            await CreateAllRolesAsync(roleManager, logger);

            // Crea l'utente amministratore di default se non esiste
            await CreateDefaultAdminUserAsync(userManager, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task CreateAllRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        var roles = new[] { MasterRoleName, LeaguePlayerRoleName, LeagueGuestRoleName };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation("Creating {RoleName} role...", roleName);
                
                var role = new IdentityRole(roleName)
                {
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("{RoleName} role created successfully", roleName);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create {RoleName} role: {Errors}", roleName, errors);
                    throw new InvalidOperationException($"Failed to create {roleName} role: {errors}");
                }
            }
            else
            {
                logger.LogInformation("{RoleName} role already exists", roleName);
            }
        }
    }

    private static async Task CreateMasterRoleAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        if (!await roleManager.RoleExistsAsync(MasterRoleName))
        {
            logger.LogInformation("Creating Master role...");
            
            var masterRole = new IdentityRole(MasterRoleName)
            {
                NormalizedName = MasterRoleName.ToUpperInvariant()
            };

            var result = await roleManager.CreateAsync(masterRole);
            if (result.Succeeded)
            {
                logger.LogInformation("Master role created successfully");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create Master role: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create Master role: {errors}");
            }
        }
        else
        {
            logger.LogInformation("Master role already exists");
        }
    }

    private static async Task CreateDefaultAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var existingUser = await userManager.FindByEmailAsync(DefaultUserEmail);
        if (existingUser == null)
        {
            logger.LogInformation("Creating default admin user...");

            var adminUser = new ApplicationUser
            {
                UserName = DefaultUserEmail,
                Email = DefaultUserEmail,
                EmailConfirmed = true,
                LockoutEnabled = false
            };

            var result = await userManager.CreateAsync(adminUser, DefaultUserPassword);
            if (result.Succeeded)
            {
                logger.LogInformation("Default admin user created successfully");

                // Assegna il ruolo Master all'utente
                var roleResult = await userManager.AddToRoleAsync(adminUser, MasterRoleName);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Master role assigned to default admin user");
                }
                else
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to assign Master role to admin user: {Errors}", roleErrors);
                    throw new InvalidOperationException($"Failed to assign Master role: {roleErrors}");
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create default admin user: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create default admin user: {errors}");
            }
        }
        else
        {
            logger.LogInformation("Default admin user already exists");

            // Verifica che abbia il ruolo Master
            if (!await userManager.IsInRoleAsync(existingUser, MasterRoleName))
            {
                logger.LogInformation("Adding Master role to existing admin user...");
                var roleResult = await userManager.AddToRoleAsync(existingUser, MasterRoleName);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Master role assigned to existing admin user");
                }
                else
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    logger.LogWarning("Failed to assign Master role to existing admin user: {Errors}", roleErrors);
                }
            }
        }
    }
}
