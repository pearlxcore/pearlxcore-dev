using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace pearlxcore.dev.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Log.Information("Starting database initialization");

        // ?? Ensure database is created / migrated
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed");

        // ?? Ensure Admin role exists
        const string adminRoleName = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            Log.Information("Creating Admin role");
            var roleResult = await roleManager.CreateAsync(new IdentityRole(adminRoleName));
            if (!roleResult.Succeeded)
            {
                Log.Error("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                throw new Exception("Failed to create Admin role: " +
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            Log.Information("Admin role created successfully");
        }

        // ?? Admin user config (email is NOT secret)
        const string adminEmail = "admin@lighthouse.local";

        // ?? Password MUST come from environment variable
        var adminPassword = Environment.GetEnvironmentVariable("LIGHTHOUSE_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            Log.Fatal("Environment variable 'LIGHTHOUSE_ADMIN_PASSWORD' is not set");
            throw new Exception(
                "Environment variable 'LIGHTHOUSE_ADMIN_PASSWORD' is not set. " +
                "Set it before running the application."
            );
        }

        // ?? Ensure Admin user exists
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            Log.Information("Creating Admin user: {Email}", adminEmail);
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                Log.Error("Failed to create Admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new Exception("Failed to create Admin user: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
            Log.Information("Admin user created successfully");
        }

        // ?? Ensure Admin role is assigned
        if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
        {
            Log.Information("Assigning Admin role to user: {Email}", adminEmail);
            var roleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!roleResult.Succeeded)
            {
                Log.Error("Failed to assign Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                throw new Exception("Failed to assign Admin role: " +
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            Log.Information("Admin role assigned successfully");
        }

        Log.Information("Database initialization completed successfully");
    }
}
