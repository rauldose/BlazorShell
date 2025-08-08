using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Services;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Linq;

namespace BlazorShell.Infrastructure.Services
{
    public class ModuleLoader : IModuleLoader
    {
        private readonly ILogger<ModuleLoader> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModuleRegistry _moduleRegistry;
        private readonly IPluginAssemblyLoader _assemblyLoader;
        private readonly ModuleRouteProvider _routeProvider;
        private readonly IModuleServiceProvider _moduleServiceProvider;
        private readonly ConcurrentDictionary<string, ModuleLoadContext> _loadContexts;
        private readonly string _modulesPath;
        private readonly IDynamicRouteService _dynamicRouteService;
        private readonly IModulePerformanceMonitor? _performanceMonitor;
        private static bool _modulesInitialized = false;
        private static readonly object _initLock = new object();

        // Add metadata cache and synchronization
        private readonly ModuleMetadataCache _metadataCache;
        private readonly SemaphoreSlim _reloadSemaphore = new(1, 1);

        public ModuleLoader(
            ILogger<ModuleLoader> logger,
            IServiceProvider serviceProvider,
            IModuleRegistry moduleRegistry,
            IPluginAssemblyLoader assemblyLoader,
            ModuleRouteProvider routeProvider,
            IModuleServiceProvider moduleServiceProvider,
            IDynamicRouteService dynamicRouteService,
            IOptions<ModuleConfiguration> options,
            ModuleMetadataCache metadataCache, // Inject metadata cache
            IModulePerformanceMonitor? performanceMonitor = null)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _moduleRegistry = moduleRegistry;
            _assemblyLoader = assemblyLoader;
            _routeProvider = routeProvider;
            _moduleServiceProvider = moduleServiceProvider;
            _loadContexts = new ConcurrentDictionary<string, ModuleLoadContext>();
            _dynamicRouteService = dynamicRouteService;
            _performanceMonitor = performanceMonitor;
            _metadataCache = metadataCache;
            _modulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.Value.ModulesPath ?? "Modules");
        }

        public async Task InitializeModulesAsync()
        {
            lock (_initLock)
            {
                // Check if modules have already been initialized in this app domain
                if (_modulesInitialized)
                {
                    _logger.LogInformation("Modules already initialized, ensuring routes are registered");
                    EnsureRoutesRegistered();
                    return;
                }
                _modulesInitialized = true;
            }

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

                            // FIX: Use scoped DbContext for database operations
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                                var dbModule = await scopedDbContext.Modules
                                    .FirstOrDefaultAsync(m => m.Name == moduleConfig.Name);

                                if (dbModule != null)
                                {
                                    dbModule.IsEnabled = false;
                                    await scopedDbContext.SaveChangesAsync();
                                }
                            }

                            continue;
                        }

                        await LoadModuleInternalAsync(assemblyPath, moduleConfig);
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

        private void EnsureRoutesRegistered()
        {
            try
            {
                var modules = _moduleRegistry.GetModules();
                foreach (var module in modules)
                {
                    // Re-register routes for each module
                    var componentTypes = module.GetComponentTypes();
                    if (componentTypes?.Any() == true)
                    {
                        _routeProvider.RegisterModuleRoutes(module.Name, componentTypes);

                        // Also register with dynamic route service
                        var assembly = module.GetType().Assembly;
                        _dynamicRouteService.RegisterModuleAssembly(module.Name, assembly);

                        _logger.LogDebug("Re-registered {Count} routes for module {Module}",
                            componentTypes.Count(), module.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring routes are registered");
            }
        }

        private async Task LoadModuleInternalAsync(string assemblyPath, ModuleConfig moduleConfig)
        {
            var module = await LoadModuleAsync(assemblyPath);
            if (module != null)
            {
                // Store metadata for future reloads
                var metadata = new ModuleMetadataCache.ModuleMetadata
                {
                    ModuleName = module.Name,
                    AssemblyPath = assemblyPath,
                    AssemblyName = moduleConfig.AssemblyName,
                    Version = module.Version,
                    LoadedAt = DateTime.UtcNow,
                    IsEnabled = true,
                    IsCore = moduleConfig.LoadOrder < 100,
                    RequiredRole = moduleConfig.RequiredRole,
                    Configuration = moduleConfig.Configuration ?? new Dictionary<string, object>(),
                    Dependencies = moduleConfig.Dependencies ?? new List<string>(),
                    CurrentState = ModuleMetadataCache.ModuleState.Loaded
                };
                _metadataCache.StoreMetadata(module.Name, metadata);

                // Register module services if it implements IServiceModule
                if (module is IServiceModule serviceModule)
                {
                    _moduleServiceProvider.RegisterModuleServices(module.Name, serviceModule);
                    _logger.LogInformation("Registered services for module {Module}", module.Name);
                }

                // Initialize module
                var initialized = await module.InitializeAsync(_serviceProvider);
                if (initialized)
                {
                    // IMPORTANT: Update database FIRST to get proper IDs
                    await UpdateModuleInDatabase(module, moduleConfig);

                    // Now load the navigation items FROM DATABASE with proper IDs
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Get the module from database
                        var dbModule = await dbContext.Modules
                            .Include(m => m.NavigationItems)
                            .FirstOrDefaultAsync(m => m.Name == module.Name);

                        if (dbModule != null && dbModule.NavigationItems?.Any() == true)
                        {
                            // Register the DATABASE navigation items (with proper IDs) with NavigationService
                            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
                            navigationService.RegisterNavigationItems(dbModule.NavigationItems.Where(ni => !ni.ParentId.HasValue));
                            _logger.LogInformation("Registered {Count} navigation items for module {Module}",
                                dbModule.NavigationItems.Count, module.Name);
                        }
                    }

                    // Register module components for routing
                    var componentTypes = module.GetComponentTypes();
                    if (componentTypes?.Any() == true)
                    {
                        _routeProvider.RegisterModuleRoutes(module.Name, componentTypes);
                        _logger.LogInformation("Registered {Count} components for module {Module}",
                            componentTypes.Count(), module.Name);
                    }

                    _logger.LogInformation("Module {Module} loaded successfully", module.Name);
                }
                else
                {
                    _logger.LogError("Failed to initialize module {Module}", moduleConfig.Name);
                    _metadataCache.UpdateState(module.Name, ModuleMetadataCache.ModuleState.Error, "Initialization failed");
                }
            }
        }

        public async Task<IModule> LoadModuleAsync(string assemblyPath)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Loading module from {Path}", assemblyPath);

                var fileName = Path.GetFileNameWithoutExtension(assemblyPath);

                // Check if already loaded
                if (_loadContexts.ContainsKey(fileName))
                {
                    _logger.LogWarning("Module {Module} is already loaded", fileName);
                    return _loadContexts[fileName].Module;
                }

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

                var componentTypes = module.GetComponentTypes()?.ToList() ?? new List<Type>();

                // Store load context for unloading
                _loadContexts[module.Name] = new ModuleLoadContext
                {
                    Assembly = assembly,
                    Module = module,
                    LoadedAt = DateTime.UtcNow,
                    ComponentTypes = componentTypes,
                    AssemblyPath = assemblyPath // Store the path for reload
                };

                // Store/update metadata
                var metadata = _metadataCache.GetMetadata(module.Name) ?? new ModuleMetadataCache.ModuleMetadata();
                metadata.ModuleName = module.Name;
                metadata.AssemblyPath = assemblyPath;
                metadata.AssemblyName = Path.GetFileName(assemblyPath);
                metadata.Version = module.Version;
                metadata.LoadedAt = DateTime.UtcNow;
                metadata.IsEnabled = true;
                metadata.CurrentState = ModuleMetadataCache.ModuleState.Loaded;
                _metadataCache.StoreMetadata(module.Name, metadata);

                // Register module
                _moduleRegistry.RegisterModule(module);

                // Register with dynamic route service
                _dynamicRouteService.RegisterModuleAssembly(module.Name, assembly);

                // Activate module
                await module.ActivateAsync();
                stopwatch.Stop();
                _performanceMonitor?.RecordModuleLoad(module.Name, stopwatch.Elapsed);

                return module;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error loading module from {Path}", assemblyPath);

                // Update metadata with error state
                var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                _metadataCache.UpdateState(fileName, ModuleMetadataCache.ModuleState.Error, ex.Message);

                return null;
            }
        }

        public async Task<bool> UnloadModuleAsync(string moduleName)
        {
            await _reloadSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Unloading module {Module}", moduleName);

                // Update metadata state
                _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Unloading);

                // Get current metadata and preserve it
                var metadata = _metadataCache.GetMetadata(moduleName);
                if (metadata == null && _loadContexts.TryGetValue(moduleName, out var ctx))
                {
                    // Create metadata from existing context
                    metadata = new ModuleMetadataCache.ModuleMetadata
                    {
                        ModuleName = moduleName,
                        AssemblyPath = ctx.AssemblyPath ?? ctx.Assembly?.Location ?? string.Empty,
                        Version = ctx.Module?.Version ?? "Unknown",
                        IsEnabled = false,
                        CurrentState = ModuleMetadataCache.ModuleState.Unloading
                    };

                    // Try to get additional info from database
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var dbModule = await scopedDbContext.Modules
                            .AsNoTracking()
                            .FirstOrDefaultAsync(m => m.Name == moduleName);

                        if (dbModule != null)
                        {
                            metadata.AssemblyPath = Path.Combine(_modulesPath, dbModule.AssemblyName);
                            metadata.AssemblyName = dbModule.AssemblyName;
                            metadata.RequiredRole = dbModule.RequiredRole;

                            // Parse configuration and dependencies if stored as JSON
                            if (!string.IsNullOrEmpty(dbModule.Configuration))
                            {
                                try
                                {
                                    metadata.Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(dbModule.Configuration)
                                        ?? new Dictionary<string, object>();
                                }
                                catch { }
                            }

                            if (!string.IsNullOrEmpty(dbModule.Dependencies))
                            {
                                try
                                {
                                    metadata.Dependencies = JsonConvert.DeserializeObject<List<string>>(dbModule.Dependencies)
                                        ?? new List<string>();
                                }
                                catch { }
                            }
                        }
                    }

                    _metadataCache.StoreMetadata(moduleName, metadata);
                }

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

                // Unregister routes
                if (_loadContexts.TryGetValue(moduleName, out var context) && context.ComponentTypes != null)
                {
                    _routeProvider.UnregisterModuleRoutes(moduleName, context.ComponentTypes);
                }
                _dynamicRouteService.UnregisterModuleAssembly(moduleName);

                // Unregister from registry
                _moduleRegistry.UnregisterModule(moduleName);

                // Unload assembly if using load context
                if (context != null)
                {
                    _assemblyLoader.UnloadPlugin(moduleName);
                    _loadContexts.TryRemove(moduleName, out _);
                }

                // Update database
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var dbModule = await scopedDbContext.Modules
                        .FirstOrDefaultAsync(m => m.Name == moduleName);

                    if (dbModule != null)
                    {
                        dbModule.IsEnabled = false;
                        await scopedDbContext.SaveChangesAsync();
                    }
                }

                // Update metadata state (keep in cache for reload)
                _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Unloaded);

                _logger.LogInformation("Module {Module} unloaded successfully", moduleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading module {Module}", moduleName);
                _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error, ex.Message);
                return false;
            }
            finally
            {
                _reloadSemaphore.Release();
            }
        }

        public async Task<IEnumerable<IModule>> GetLoadedModulesAsync()
        {
            return await Task.FromResult(_moduleRegistry.GetModules());
        }

        public async Task<IModule> GetModuleAsync(string moduleName)
        {
            return await Task.FromResult(_moduleRegistry.GetModule(moduleName));
        }

        public async Task ReloadModuleAsync(string moduleName)
        {
            await _reloadSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Starting reload of module {Module}", moduleName);

                // Update state to reloading
                _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Reloading);

                // Get metadata (should be preserved from unload)
                var metadata = _metadataCache.GetMetadata(moduleName);
                string assemblyPath = null;

                // Priority 1: Use cached metadata
                if (metadata != null && !string.IsNullOrEmpty(metadata.AssemblyPath))
                {
                    assemblyPath = metadata.AssemblyPath;
                    _logger.LogDebug("Using cached assembly path: {Path}", assemblyPath);
                }

                // Priority 2: Get from current load context
                if (string.IsNullOrEmpty(assemblyPath) && _loadContexts.TryGetValue(moduleName, out var context))
                {
                    assemblyPath = context.AssemblyPath ?? context.Assembly?.Location;
                    _logger.LogDebug("Using load context assembly path: {Path}", assemblyPath);
                }

                // Priority 3: Get from database
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var dbModule = await scopedDbContext.Modules
                            .AsNoTracking()
                            .FirstOrDefaultAsync(m => m.Name == moduleName);

                        if (dbModule != null)
                        {
                            assemblyPath = Path.Combine(_modulesPath, dbModule.AssemblyName);
                            _logger.LogDebug("Using database assembly path: {Path}", assemblyPath);
                        }
                    }
                }

                if (string.IsNullOrEmpty(assemblyPath))
                {
                    _logger.LogError("Cannot find assembly path for module {Module}", moduleName);
                    _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error,
                        "Assembly path not found");
                    return;
                }

                // Verify file exists
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogError("Assembly file not found at {Path}", assemblyPath);
                    _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error,
                        $"Assembly file not found: {assemblyPath}");
                    return;
                }

                // Unload if currently loaded
                if (_moduleRegistry.GetModule(moduleName) != null)
                {
                    _logger.LogDebug("Unloading module {Module} before reload", moduleName);
                    var unloadSuccess = await UnloadModuleAsync(moduleName);
                    if (!unloadSuccess)
                    {
                        _logger.LogError("Failed to unload module {Module} for reload", moduleName);
                        _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error,
                            "Failed to unload for reload");
                        return;
                    }

                    // Wait for cleanup
                    await Task.Delay(500);

                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                // Reload the module
                _logger.LogDebug("Loading module {Module} from {Path}", moduleName, assemblyPath);
                var module = await LoadModuleAsync(assemblyPath);

                if (module != null)
                {
                    // Restore configuration if available
                    if (metadata?.Configuration != null)
                    {
                        // Create ModuleConfig for initialization
                        var config = new ModuleConfig
                        {
                            Name = moduleName,
                            AssemblyName = metadata.AssemblyName ?? Path.GetFileName(assemblyPath),
                            Version = module.Version,
                            Dependencies = metadata.Dependencies,
                            Configuration = metadata.Configuration,
                            Enabled = true
                        };

                        // Initialize with restored configuration
                        await LoadModuleInternalAsync(assemblyPath, config);
                    }

                    _logger.LogInformation("Module {Module} reloaded successfully", moduleName);
                    _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Loaded);
                }
                else
                {
                    _logger.LogError("Failed to reload module {Module}", moduleName);
                    _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error,
                        "Reload failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading module {Module}", moduleName);
                _metadataCache.UpdateState(moduleName, ModuleMetadataCache.ModuleState.Error, ex.Message);
            }
            finally
            {
                _reloadSemaphore.Release();
            }
        }

        private async Task UpdateModuleInDatabase(IModule module, ModuleConfig config)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbModule = await scopedDbContext.Modules
                    .Include(m => m.NavigationItems)
                    .FirstOrDefaultAsync(m => m.Name == module.Name);

                bool isNewModule = false;
                if (dbModule == null)
                {
                    isNewModule = true;
                    dbModule = new BlazorShell.Domain.Entities.Module
                    {
                        Name = module.Name,
                        DisplayName = module.DisplayName,
                        Description = module.Description,
                        Version = module.Version,
                        Author = module.Author,
                        Icon = module.Icon,
                        Category = module.Category,
                        RequiredRole = config.RequiredRole,
                        LoadOrder = module.Order,
                        IsEnabled = true,
                        AssemblyName = config.AssemblyName,
                        EntryType = config.EntryType,
                        Configuration = JsonConvert.SerializeObject(config.Configuration),
                        Dependencies = JsonConvert.SerializeObject(config.Dependencies),
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System"
                    };
                    scopedDbContext.Modules.Add(dbModule);
                    await scopedDbContext.SaveChangesAsync(); // Get the Module ID
                }
                else
                {
                    // Update existing module
                    dbModule.DisplayName = module.DisplayName;
                    dbModule.Description = module.Description;
                    dbModule.Version = module.Version;
                    dbModule.Author = module.Author;
                    dbModule.Icon = module.Icon;
                    dbModule.Category = module.Category;
                    dbModule.RequiredRole = config.RequiredRole;
                    dbModule.LoadOrder = module.Order;
                    dbModule.IsEnabled = true;
                    dbModule.AssemblyName = config.AssemblyName;
                    dbModule.EntryType = config.EntryType;
                    dbModule.Configuration = JsonConvert.SerializeObject(config.Configuration);
                    dbModule.Dependencies = JsonConvert.SerializeObject(config.Dependencies);
                    dbModule.ModifiedDate = DateTime.UtcNow;
                    dbModule.ModifiedBy = "System";
                    await scopedDbContext.SaveChangesAsync();
                }

                // Handle navigation items
                var moduleNavItems = module.GetNavigationItems()?.ToList() ?? new List<NavigationItem>();

                if (moduleNavItems.Any())
                {
                    // Clear existing navigation items if this is a re-registration
                    if (!isNewModule && dbModule.NavigationItems?.Any() == true)
                    {
                        scopedDbContext.NavigationItems.RemoveRange(dbModule.NavigationItems);
                        await scopedDbContext.SaveChangesAsync();
                    }

                    // Group items by whether they have a parent
                    var rootItems = moduleNavItems.Where(n => !moduleNavItems.Any(p => p.Children?.Contains(n) ?? false)).ToList();
                    var childItemsMap = new Dictionary<NavigationItem, List<NavigationItem>>();

                    foreach (var item in rootItems)
                    {
                        if (item.Children != null && item.Children.Any())
                        {
                            childItemsMap[item] = item.Children.ToList();
                        }
                    }

                    // Save root items first
                    var savedItemsMap = new Dictionary<string, NavigationItem>();

                    foreach (var item in rootItems)
                    {
                        var dbItem = new NavigationItem
                        {
                            ModuleId = dbModule.Id,
                            Name = item.Name,
                            DisplayName = item.DisplayName,
                            Url = item.Url,
                            Icon = item.Icon,
                            Order = item.Order,
                            IsVisible = item.IsVisible,
                            IsPublic = item.IsPublic,
                            MinimumRole = item.MinimumRole,
                            Target = item.Target,
                            CssClass = item.CssClass,
                            Type = item.Type,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = "System"
                        };

                        scopedDbContext.NavigationItems.Add(dbItem);
                        savedItemsMap[item.Name ?? Guid.NewGuid().ToString()] = dbItem;
                    }

                    await scopedDbContext.SaveChangesAsync(); // Get IDs for root items

                    // Now save child items with proper parent IDs
                    foreach (var kvp in childItemsMap)
                    {
                        var parentItem = savedItemsMap[kvp.Key.Name ?? ""];

                        foreach (var childItem in kvp.Value)
                        {
                            var dbChild = new NavigationItem
                            {
                                ModuleId = dbModule.Id,
                                ParentId = parentItem.Id, // Now we have the real parent ID
                                Name = childItem.Name,
                                DisplayName = childItem.DisplayName,
                                Url = childItem.Url,
                                Icon = childItem.Icon,
                                Order = childItem.Order,
                                IsVisible = childItem.IsVisible,
                                IsPublic = childItem.IsPublic,
                                MinimumRole = childItem.MinimumRole,
                                Target = childItem.Target,
                                CssClass = childItem.CssClass,
                                Type = childItem.Type,
                                CreatedDate = DateTime.UtcNow,
                                CreatedBy = "System"
                            };

                            scopedDbContext.NavigationItems.Add(dbChild);
                        }
                    }

                    await scopedDbContext.SaveChangesAsync(); // Save child items
                }
            }
        }

        private NavigationItem CreateNavigationItem(NavigationItem sourceItem, int moduleId)
        {
            return new NavigationItem
            {
                ModuleId = moduleId,
                Name = sourceItem.Name,
                DisplayName = sourceItem.DisplayName,
                Url = sourceItem.Url,
                Icon = sourceItem.Icon,
                Order = sourceItem.Order,
                IsVisible = sourceItem.IsVisible,
                IsPublic = sourceItem.IsPublic,
                MinimumRole = sourceItem.MinimumRole,
                Target = sourceItem.Target,
                CssClass = sourceItem.CssClass,
                Type = sourceItem.Type,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            };
        }

        private void UpdateNavigationItem(NavigationItem dbItem, NavigationItem sourceItem)
        {
            dbItem.DisplayName = sourceItem.DisplayName;
            dbItem.Url = sourceItem.Url;
            dbItem.Icon = sourceItem.Icon;
            dbItem.Order = sourceItem.Order;
            dbItem.IsVisible = sourceItem.IsVisible;
            dbItem.IsPublic = sourceItem.IsPublic;
            dbItem.MinimumRole = sourceItem.MinimumRole;
            dbItem.Target = sourceItem.Target;
            dbItem.CssClass = sourceItem.CssClass;
            dbItem.Type = sourceItem.Type;
            dbItem.ModifiedDate = DateTime.UtcNow;
            dbItem.ModifiedBy = "System";
        }

        private class ModuleLoadContext
        {
            public Assembly Assembly { get; set; }
            public IModule Module { get; set; }
            public DateTime LoadedAt { get; set; }
            public List<Type> ComponentTypes { get; set; }
            public string AssemblyPath { get; set; } // Added to store the path
        }
    }

    // Keep your existing configuration classes as they are
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