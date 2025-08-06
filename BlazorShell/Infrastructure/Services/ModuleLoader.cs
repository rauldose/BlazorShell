using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Module = BlazorShell.Core.Entities.Module;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Implementation of module loader with assembly isolation
    /// </summary>
    public class ModuleLoader : IModuleLoader
    {
        private readonly ILogger<ModuleLoader> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModuleRegistry _moduleRegistry;
        private readonly IPluginAssemblyLoader _assemblyLoader;
        private readonly ApplicationDbContext _dbContext;
        private readonly Dictionary<string, ModuleLoadContext> _loadContexts;
        private readonly string _modulesPath;
        private readonly IServiceCollection _services;

        public ModuleLoader(
            ILogger<ModuleLoader> logger,
            IServiceProvider serviceProvider,
            IModuleRegistry moduleRegistry,
            IPluginAssemblyLoader assemblyLoader,
            ApplicationDbContext dbContext,
            IOptions<ModuleConfiguration> options,
            IServiceCollection services)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _moduleRegistry = moduleRegistry;
            _assemblyLoader = assemblyLoader;
            _dbContext = dbContext;
            _loadContexts = new Dictionary<string, ModuleLoadContext>();
            _modulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.Value.ModulesPath ?? "Modules");
            _services = services;
        }

        public async Task InitializeModulesAsync()
        {
            try
            {
                _logger.LogInformation("Initializing modules from configuration...");

                // Load configuration from JSON file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.json");
                if (!File.Exists(configPath))
                {
                    _logger.LogWarning("Module configuration file not found at {Path}", configPath);
                    return;
                }

                var configJson = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<ModulesConfiguration>(configJson);

                if (config?.Modules == null)
                {
                    _logger.LogWarning("No modules found in configuration");
                    return;
                }

                // Ensure modules directory exists
                if (!Directory.Exists(_modulesPath))
                {
                    Directory.CreateDirectory(_modulesPath);
                }

                // Sort modules by load order
                var sortedModules = config.Modules
                    .Where(m => m.Enabled)
                    .OrderBy(m => m.LoadOrder)
                    .ToList();

                foreach (var moduleConfig in sortedModules)
                {
                    try
                    {
                        // Check dependencies
                        if (moduleConfig.Dependencies?.Any() == true)
                        {
                            foreach (var dependency in moduleConfig.Dependencies)
                            {
                                if (!_moduleRegistry.IsModuleRegistered(dependency))
                                {
                                    _logger.LogWarning(
                                        "Module {Module} depends on {Dependency} which is not loaded",
                                        moduleConfig.Name, dependency);
                                    continue;
                                }
                            }
                        }

                        var assemblyPath = Path.Combine(_modulesPath, moduleConfig.AssemblyName);
                        if (!File.Exists(assemblyPath))
                        {
                            _logger.LogWarning("Module assembly not found: {Path}", assemblyPath);

                            // Update database to reflect module is not available
                            var dbModule = await _dbContext.Modules
                                .FirstOrDefaultAsync(m => m.Name == moduleConfig.Name);
                            if (dbModule != null)
                            {
                                dbModule.IsEnabled = false;
                                await _dbContext.SaveChangesAsync();
                            }
                            continue;
                        }

                        var module = await LoadModuleAsync(assemblyPath);
                        if (module != null)
                        {
                            // Initialize module
                            var initialized = await module.InitializeAsync(_serviceProvider);
                            if (initialized)
                            {
                                // Register navigation items
                                var navItems = module.GetNavigationItems();
                                if (navItems?.Any() == true)
                                {
                                    var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
                                    navigationService.RegisterNavigationItems(navItems);
                                }

                                // Update or create database entry
                                await UpdateModuleInDatabase(module, moduleConfig);

                                _logger.LogInformation("Module {Module} loaded successfully", module.Name);
                            }
                            else
                            {
                                _logger.LogError("Failed to initialize module {Module}", moduleConfig.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading module {Module}", moduleConfig.Name);
                    }
                }

                _logger.LogInformation("Module initialization completed. {Count} modules loaded",
                    _moduleRegistry.GetModules().Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during module initialization");
                throw;
            }
        }

        public async Task<IModule> LoadModuleAsync(string assemblyPath)
        {
            try
            {
                _logger.LogDebug("Loading module from {Path}", assemblyPath);

                // Load assembly
                var assembly = _assemblyLoader.LoadPlugin(assemblyPath);
                if (assembly == null)
                {
                    _logger.LogError("Failed to load assembly from {Path}", assemblyPath);
                    return null;
                }

                // Find module type
                var moduleTypes = _assemblyLoader.GetTypesFromAssembly(assembly, typeof(IModule));
                var moduleType = moduleTypes.FirstOrDefault();

                if (moduleType == null)
                {
                    _logger.LogError("No IModule implementation found in {Assembly}", assembly.FullName);
                    return null;
                }

                // Create module instance
                var module = _assemblyLoader.CreateInstance<IModule>(moduleType);
                if (module == null)
                {
                    _logger.LogError("Failed to create instance of {Type}", moduleType.FullName);
                    return null;
                }

                if (module is IServiceModule serviceModule)
                {
                    serviceModule.RegisterServices(_services);
                }

                // Store load context for unloading
                _loadContexts[module.Name] = new ModuleLoadContext
                {
                    Assembly = assembly,
                    Module = module,
                    LoadedAt = DateTime.UtcNow
                };

                // Register module
                _moduleRegistry.RegisterModule(module);

                // Activate module
                await module.ActivateAsync();

                return module;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading module from {Path}", assemblyPath);
                return null;
            }
        }

        public async Task<bool> UnloadModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Unloading module {Module}", moduleName);

                var module = _moduleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    _logger.LogWarning("Module {Module} not found in registry", moduleName);
                    return false;
                }

                // Deactivate module
                await module.DeactivateAsync();

                // Unregister navigation items
                var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
                navigationService.UnregisterNavigationItems(moduleName);

                // Unregister from registry
                _moduleRegistry.UnregisterModule(moduleName);

                // Unload assembly if using load context
                if (_loadContexts.TryGetValue(moduleName, out var context))
                {
                    _assemblyLoader.UnloadPlugin(moduleName);
                    _loadContexts.Remove(moduleName);
                }

                // Update database
                var dbModule = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName);
                if (dbModule != null)
                {
                    dbModule.IsEnabled = false;
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Module {Module} unloaded successfully", moduleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading module {Module}", moduleName);
                return false;
            }
        }

        public async Task<IEnumerable<IModule>> GetLoadedModulesAsync()
        {
            return await Task.FromResult(_moduleRegistry.GetModules());
        }

        public async Task<IEnumerable<Assembly>> GetModuleAssembliesAsync()
        {
            return await Task.FromResult(_loadContexts.Values.Select(c => c.Assembly));
        }

        public async Task<IModule> GetModuleAsync(string moduleName)
        {
            return await Task.FromResult(_moduleRegistry.GetModule(moduleName));
        }

        public async Task ReloadModuleAsync(string moduleName)
        {
            _logger.LogInformation("Reloading module {Module}", moduleName);

            // Get current module info
            var currentModule = _moduleRegistry.GetModule(moduleName);
            if (currentModule == null)
            {
                _logger.LogWarning("Module {Module} not found for reload", moduleName);
                return;
            }

            // Store assembly path
            string assemblyPath = null;
            if (_loadContexts.TryGetValue(moduleName, out var context))
            {
                assemblyPath = context.Assembly.Location;
            }

            // Unload module
            await UnloadModuleAsync(moduleName);

            // Wait a bit for cleanup
            await Task.Delay(500);

            // Reload module
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                await LoadModuleAsync(assemblyPath);
            }
        }

        private async Task UpdateModuleInDatabase(IModule module, ModuleConfig config)
        {
            var dbModule = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == module.Name);

            if (dbModule == null)
            {
                dbModule = new Module
                {
                    Name = module.Name,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                _dbContext.Modules.Add(dbModule);
            }

            dbModule.DisplayName = module.DisplayName;
            dbModule.Description = module.Description;
            dbModule.Version = module.Version;
            dbModule.Author = module.Author;
            dbModule.Icon = module.Icon;
            dbModule.Category = module.Category;
            dbModule.LoadOrder = module.Order;
            dbModule.IsEnabled = true;
            dbModule.AssemblyName = config.AssemblyName;
            dbModule.EntryType = config.EntryType;
            dbModule.Configuration = JsonConvert.SerializeObject(config.Configuration);
            dbModule.Dependencies = JsonConvert.SerializeObject(config.Dependencies);
            dbModule.ModifiedDate = DateTime.UtcNow;
            dbModule.ModifiedBy = "System";

            await _dbContext.SaveChangesAsync();
        }

        private class ModuleLoadContext
        {
            public Assembly Assembly { get; set; }
            public IModule Module { get; set; }
            public DateTime LoadedAt { get; set; }
        }
    }

    /// <summary>
    /// Configuration classes for JSON deserialization
    /// </summary>
    public class ModulesConfiguration
    {
        public ModuleSettings ModuleSettings { get; set; }
        public List<ModuleConfig> Modules { get; set; }
    }

    public class ModuleSettings
    {
        public bool EnableDynamicLoading { get; set; }
        public string ModulesPath { get; set; }
        public bool AllowRemoteModules { get; set; }
        public bool AutoLoadOnStartup { get; set; }
        public bool CacheModuleMetadata { get; set; }
    }

    public class ModuleConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string AssemblyName { get; set; }
        public string EntryType { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public bool Enabled { get; set; }
        public int LoadOrder { get; set; }
        public List<string> Dependencies { get; set; }
        public string RequiredRole { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<NavigationItemConfig> NavigationItems { get; set; }
        public List<PermissionConfig> Permissions { get; set; }
    }

    public class NavigationItemConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public string RequiredPermission { get; set; }
        public string Parent { get; set; }
        public List<NavigationItemConfig> Children { get; set; }
    }

    public class PermissionConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public class ModuleConfiguration
    {
        public string ModulesPath { get; set; } = "Modules";
    }
}