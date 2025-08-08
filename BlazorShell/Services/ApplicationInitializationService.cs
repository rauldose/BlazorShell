using BlazorShell.Domain.Entities;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Services;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BlazorShell.Services;

public class ApplicationInitializationService : IApplicationInitializationService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILazyModuleLoader _lazyLoader;
    private readonly IEnumerable<IHostedService> _hostedServices;
    private readonly ILogger<ApplicationInitializationService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ApplicationInitializationService(
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IModuleRegistry moduleRegistry,
        ILazyModuleLoader lazyLoader,
        IEnumerable<IHostedService> hostedServices,
        ILogger<ApplicationInitializationService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _moduleRegistry = moduleRegistry;
        _lazyLoader = lazyLoader;
        _hostedServices = hostedServices;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        if (_environment.IsProduction())
        {
            await _context.Database.MigrateAsync();
        }
        else
        {
            await _context.Database.EnsureCreatedAsync();
        }

        await SeedRoles();

        if (_environment.IsDevelopment())
        {
            await SeedDefaultAdmin();
        }

        var loadedCount = _moduleRegistry.GetModules().Count();
        var totalCount = _lazyLoader.GetAllModuleStatuses().Count();

        _logger.LogInformation(
            "Application initialized with {LoadedCount}/{TotalCount} modules loaded (lazy loading enabled)",
            loadedCount, totalCount);

        if (_hostedServices.OfType<ModuleCleanupService>().Any())
        {
            _logger.LogInformation("Module cleanup service started");
        }
    }

    private async Task SeedRoles()
    {
        var roles = new[]
        {
            new ApplicationRole { Name = "Administrator", Description = "Full system access", IsSystemRole = true },
            new ApplicationRole { Name = "ModuleAdmin", Description = "Can manage modules", IsSystemRole = true },
            new ApplicationRole { Name = "User", Description = "Standard user access", IsSystemRole = true }
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name!))
            {
                await _roleManager.CreateAsync(role);
            }
        }
    }

    private async Task SeedDefaultAdmin()
    {
        const string adminEmail = "admin@blazorshell.local";
        var adminPassword = _configuration["DefaultAdmin:Password"] ?? throw new InvalidOperationException();

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
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

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }
    }
}

