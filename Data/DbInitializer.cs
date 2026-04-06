using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace pearlxcore.dev.Data;

public static class DbInitializer
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Log.Information("Starting database migration");
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed");
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Log.Information("Starting database initialization");

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

        // ?? Admin user config - read from user-secrets / environment variables
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            Log.Fatal("Admin email is not configured. Set 'AdminUser:Email' in user-secrets or environment variables");
            throw new Exception(
                "Admin email is not configured. " +
                "Set 'AdminUser:Email' in user-secrets (development) or environment variables (production). " +
                "Example: dotnet user-secrets set \"AdminUser:Email\" \"admin@yourdomain.com\""
            );
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            Log.Fatal("Admin password is not configured. Set 'AdminUser:Password' in user-secrets or environment variables");
            throw new Exception(
                "Admin password is not configured. " +
                "Set 'AdminUser:Password' in user-secrets (development) or environment variables (production). " +
                "Example: dotnet user-secrets set \"AdminUser:Password\" \"YourSecurePassword123!\""
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
        else
        {
            // ?? User exists - update password if credentials changed
            Log.Information("Admin user already exists: {Email}. Updating password.", adminEmail);
            
            // Remove old password
            var removePasswordResult = await userManager.RemovePasswordAsync(adminUser);
            if (!removePasswordResult.Succeeded)
            {
                Log.Warning("Failed to remove old password: {Errors}", string.Join(", ", removePasswordResult.Errors.Select(e => e.Description)));
            }

            // Set new password
            var setPasswordResult = await userManager.AddPasswordAsync(adminUser, adminPassword);
            if (!setPasswordResult.Succeeded)
            {
                Log.Error("Failed to set new admin password: {Errors}", string.Join(", ", setPasswordResult.Errors.Select(e => e.Description)));
                throw new Exception("Failed to update admin password: " +
                    string.Join(", ", setPasswordResult.Errors.Select(e => e.Description)));
            }
            Log.Information("Admin password updated successfully");
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
