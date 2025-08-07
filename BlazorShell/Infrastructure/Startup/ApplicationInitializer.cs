using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Core.Entities;

namespace BlazorShell.Infrastructure.Startup;

public class ApplicationInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Apply migrations
            if (app.Environment.IsProduction())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Seed roles
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            await SeedRolesAsync(roleManager);

            // Seed default admin user (only in development)
            if (app.Environment.IsDevelopment())
            {
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                await SeedDefaultAdminAsync(userManager, roleManager);
            }

            // Log initialization completion
            var logger = services.GetRequiredService<ILogger<ApplicationInitializer>>();
            logger.LogInformation("Application initialization completed successfully");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationInitializer>>();
            logger.LogError(ex, "An error occurred while initializing the application");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            new ApplicationRole { Name = "Administrator", Description = "Full system access", IsSystemRole = true },
            new ApplicationRole { Name = "ModuleAdmin", Description = "Can manage modules", IsSystemRole = true },
            new ApplicationRole { Name = "User", Description = "Standard user access", IsSystemRole = true }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedDefaultAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        const string adminEmail = "admin@blazorshell.local";
        const string adminPassword = "Admin@123456";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }
    }
}