using HabitTracker.Api.Database;
using HabitTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using IServiceScope scope =  app.Services.CreateScope();
        await using ApplicationDbContext applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using ApplicationIdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        try
        {
            await applicationDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Application Database migrations applied successfully.");

            await identityDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Identity Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }

    public static async Task SeedRolesAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = [Roles.Admin, Roles.User];

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    app.Logger.LogInformation("Role '{Role}' created successfully.", role);
                }
                else
                {
                    app.Logger.LogError("Error creating role '{Role}': {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                app.Logger.LogInformation("Role '{Role}' already exists.", role);
            }
        }
    }
}
