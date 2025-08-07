using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Core.Entities;

namespace BlazorShell.Infrastructure.Services
{
    public interface IDatabaseInitializationService
    {
        Task InitializeDatabaseAsync(IServiceProvider services);
    }

    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            IWebHostEnvironment environment,
            ILogger<DatabaseInitializationService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Apply migrations
                if (_environment.IsProduction())
                {
                    _logger.LogInformation("Applying database migrations");
                    await context.Database.MigrateAsync();
                }
                else
                {
                    _logger.LogInformation("Ensuring database is created");
                    await context.Database.EnsureCreatedAsync();
                }

                // Seed roles
                var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
                await SeedRolesAsync(roleManager);

                // Seed default admin user (only in development)
                if (_environment.IsDevelopment())
                {
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    await SeedDefaultAdminAsync(userManager, roleManager);
                }

                _logger.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization");
                throw;
            }
        }

        private async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            _logger.LogInformation("Seeding application roles");

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
                    _logger.LogInformation("Created role: {RoleName}", role.Name);
                }
            }
        }

        private async Task SeedDefaultAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _logger.LogInformation("Seeding default admin user");

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
                    _logger.LogInformation("Default admin user created successfully");
                }
                else
                {
                    _logger.LogError("Failed to create default admin user: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}