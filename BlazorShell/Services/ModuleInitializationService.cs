using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Services;
using BlazorShell.Application.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Services;

public class ModuleInitializationService : IModuleInitializationService
{
    private readonly ILazyModuleLoader _lazyLoader;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILogger<ModuleInitializationService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IModuleHotReloadService _hotReload;

    public ModuleInitializationService(
        ILazyModuleLoader lazyLoader,
        IModuleRegistry moduleRegistry,
        ILogger<ModuleInitializationService> logger,
        IWebHostEnvironment environment,
        IModuleHotReloadService hotReload)
    {
        _lazyLoader = lazyLoader;
        _moduleRegistry = moduleRegistry;
        _logger = logger;
        _environment = environment;
        _hotReload = hotReload;
    }

    public async Task InitializeAsync()
    {
        var loadingStrategy = _environment.IsDevelopment()
            ? ModuleLoadingStrategy.PreloadCore
            : ModuleLoadingStrategy.OnDemand;

        _lazyLoader.SetModuleLoadingStrategy(loadingStrategy);
        _logger.LogInformation("Initializing with lazy loading strategy: {Strategy}", loadingStrategy);

        await _lazyLoader.LoadModuleOnDemandAsync("Admin");
        if (_environment.IsDevelopment())
        {
            await _lazyLoader.LoadModuleOnDemandAsync("Dashboard");
        }

        _logger.LogInformation("Lazy initialization complete. Loaded modules: {Count}",
            _moduleRegistry.GetModules().Count());

        if (_environment.IsDevelopment())
        {
            var modules = _moduleRegistry.GetModules();
            foreach (var module in modules)
            {
                var assemblyPath = module.GetType().Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    await _hotReload.StartWatchingAsync(module.Name, assemblyPath);
                    _logger.LogInformation("Hot reload watching enabled for {Module}", module.Name);
                }
            }
        }
    }
}

