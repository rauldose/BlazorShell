using BlazorShell.Core.Interfaces;

namespace BlazorShell.Infrastructure.Services
{
    public interface IModuleInitializationService
    {
        Task InitializeModulesAsync(IServiceProvider services);
    }

    public class ModuleInitializationService : IModuleInitializationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ModuleInitializationService> _logger;

        public ModuleInitializationService(
            IWebHostEnvironment environment,
            ILogger<ModuleInitializationService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task InitializeModulesAsync(IServiceProvider services)
        {
            try
            {
                _logger.LogInformation("Starting module initialization");

                // NEW: Use lazy loader for initialization
                var lazyLoader = services.GetRequiredService<ILazyModuleLoader>();
                var moduleRegistry = services.GetRequiredService<IModuleRegistry>();

                // Set loading strategy based on environment
                var loadingStrategy = _environment.IsDevelopment()
                    ? ModuleLoadingStrategy.PreloadCore  // In dev, only load core modules
                    : ModuleLoadingStrategy.OnDemand;     // In prod, pure lazy loading

                lazyLoader.SetModuleLoadingStrategy(loadingStrategy);

                // Only preload absolutely essential modules
                _logger.LogInformation("Initializing with lazy loading strategy: {Strategy}", loadingStrategy);

                // Load ONLY the Admin module at startup (it's required for module management)
                await lazyLoader.LoadModuleOnDemandAsync("Admin");

                // Optionally preload Dashboard if it's your default landing page
                if (_environment.IsDevelopment())
                {
                    await lazyLoader.LoadModuleOnDemandAsync("Dashboard");
                }

                _logger.LogInformation("Lazy initialization complete. Loaded modules: {Count}",
                    moduleRegistry.GetModules().Count());

                // Start hot reload monitoring in development
                if (_environment.IsDevelopment())
                {
                    await StartHotReloadMonitoringAsync(services, moduleRegistry);
                }

                _logger.LogInformation("Module initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during module initialization");
                throw;
            }
        }

        private async Task StartHotReloadMonitoringAsync(IServiceProvider services, IModuleRegistry moduleRegistry)
        {
            try
            {
                var hotReload = services.GetRequiredService<IModuleHotReloadService>();
                var modules = moduleRegistry.GetModules();

                foreach (var module in modules)
                {
                    var assemblyPath = module.GetType().Assembly.Location;
                    if (!string.IsNullOrEmpty(assemblyPath))
                    {
                        await hotReload.StartWatchingAsync(module.Name, assemblyPath);
                        _logger.LogInformation("Hot reload watching enabled for {Module}", module.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start hot reload monitoring, continuing without it");
            }
        }
    }
}