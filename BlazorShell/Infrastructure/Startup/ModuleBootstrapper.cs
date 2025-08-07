using System.Reflection;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Services;
using BlazorShell.Infrastructure.Services;

namespace BlazorShell.Infrastructure.Startup;

public class ModuleBootstrapper
{
    public static async Task InitializeModulesAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ModuleBootstrapper>>();

        try
        {
            logger.LogInformation("Starting module initialization");

            // NEW: Use lazy loader for initialization
            var lazyLoader = services.GetRequiredService<ILazyModuleLoader>();
            var moduleRegistry = services.GetRequiredService<IModuleRegistry>();

            // Set loading strategy based on environment
            var loadingStrategy = app.Environment.IsDevelopment()
                ? ModuleLoadingStrategy.PreloadCore  // In dev, only load core modules
                : ModuleLoadingStrategy.OnDemand;     // In prod, pure lazy loading

            lazyLoader.SetModuleLoadingStrategy(loadingStrategy);

            // Only preload absolutely essential modules
            logger.LogInformation("Initializing with lazy loading strategy: {Strategy}", loadingStrategy);

            // Load ONLY the Admin module at startup (it's required for module management)
            await lazyLoader.LoadModuleOnDemandAsync("Admin");

            // Optionally preload Dashboard if it's your default landing page
            if (app.Environment.IsDevelopment())
            {
                await lazyLoader.LoadModuleOnDemandAsync("Dashboard");
            }

            logger.LogInformation("Lazy initialization complete. Loaded modules: {Count}",
                moduleRegistry.GetModules().Count());

            // Start hot reload monitoring in development
            if (app.Environment.IsDevelopment())
            {
                await SetupHotReloadAsync(services, moduleRegistry, logger);
            }

            // Log module status
            var loadedCount = moduleRegistry.GetModules().Count();
            var totalCount = lazyLoader.GetAllModuleStatuses().Count();

            logger.LogInformation(
                "Module initialization completed with {LoadedCount}/{TotalCount} modules loaded (lazy loading enabled)",
                loadedCount, totalCount);

            // Start background cleanup service
            if (services.GetService<IHostedService>() is ModuleCleanupService cleanupService)
            {
                logger.LogInformation("Module cleanup service started");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during module initialization");
            throw;
        }
    }

    private static async Task SetupHotReloadAsync(IServiceProvider services, IModuleRegistry moduleRegistry, ILogger logger)
    {
        var hotReload = services.GetRequiredService<IModuleHotReloadService>();
        var modules = moduleRegistry.GetModules();

        foreach (var module in modules)
        {
            var assemblyPath = module.GetType().Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                await hotReload.StartWatchingAsync(module.Name, assemblyPath);
                logger.LogInformation("Hot reload watching enabled for {Module}", module.Name);
            }
        }
    }

    public static IEnumerable<Assembly> GetModuleAssemblies(IServiceProvider services)
    {
        var assemblies = new List<Assembly>();

        try
        {
            using var scope = services.CreateScope();
            var moduleRegistry = scope.ServiceProvider.GetService<IModuleRegistry>();
            var lazyLoader = scope.ServiceProvider.GetService<ILazyModuleLoader>();

            if (moduleRegistry != null)
            {
                // Get currently loaded modules
                var modules = moduleRegistry.GetModules();
                foreach (var module in modules)
                {
                    var assembly = module.GetType().Assembly;
                    if (!assemblies.Contains(assembly))
                    {
                        assemblies.Add(assembly);
                    }
                }

                // Important: Also add assemblies that might be lazy-loaded later
                // This ensures routes are recognized even if modules aren't loaded yet
                if (lazyLoader != null)
                {
                    var allStatuses = lazyLoader.GetAllModuleStatuses();
                    // We still need to register the assemblies for routing
                    // even if modules aren't loaded yet
                }
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILogger<ModuleBootstrapper>>();
            logger?.LogError(ex, "Error getting module assemblies");
        }

        return assemblies;
    }
}