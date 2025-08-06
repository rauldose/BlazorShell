// Infrastructure/Services/LazyModuleLoader.cs
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using BlazorShell.Application.Interfaces;
using Newtonsoft.Json;
using BlazorShell.Infrastructure.Services;

namespace BlazorShell.ModuleSystem.Services
{
    public interface ILazyModuleLoader
    {
        Task<IModule?> LoadModuleOnDemandAsync(string moduleName);
        Task PreloadModulesAsync(params string[] moduleNames);
        Task<bool> IsModuleLoadedAsync(string moduleName);
        Task<ModuleLoadStatus> GetModuleStatusAsync(string moduleName);
        void SetModuleLoadingStrategy(ModuleLoadingStrategy strategy);
        Task UnloadInactiveModulesAsync(TimeSpan inactiveThreshold);
        IEnumerable<ModuleLoadStatus> GetAllModuleStatuses();
    }

    public class LazyModuleLoader : ILazyModuleLoader
    {
        private readonly IModuleLoader _moduleLoader;
        private readonly IModuleRegistry _moduleRegistry;
        private readonly IPluginAssemblyLoader _assemblyLoader;
        private readonly IModuleServiceProvider _moduleServiceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LazyModuleLoader> _logger;
        private readonly string _modulesPath;
        private readonly IModulePerformanceMonitor? _performanceMonitor;
        private readonly ConcurrentDictionary<string, ModuleLoadStatus> _moduleStatuses = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadingSemaphores = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastAccessTimes = new();

        private ModuleLoadingStrategy _loadingStrategy = ModuleLoadingStrategy.OnDemand;
        private readonly List<ModuleConfig> _moduleConfigs = new();

        public LazyModuleLoader(
            IModuleLoader moduleLoader,
            IModuleRegistry moduleRegistry,
            IPluginAssemblyLoader assemblyLoader,
            IModuleServiceProvider moduleServiceProvider,
            IMemoryCache cache,
            ILogger<LazyModuleLoader> logger,
            IModulePerformanceMonitor? performanceMonitor = null)
        {
            _moduleLoader = moduleLoader;
            _moduleRegistry = moduleRegistry;
            _assemblyLoader = assemblyLoader;
            _moduleServiceProvider = moduleServiceProvider;
            _cache = cache;
            _logger = logger;
            _modulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");
            _performanceMonitor = performanceMonitor;
            LoadModuleConfigurations();
        }

        private void LoadModuleConfigurations()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.json");
                if (File.Exists(configPath))
                {
                    var configJson = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ModulesConfiguration>(configJson);
                    if (config?.Modules != null)
                    {
                        _moduleConfigs.AddRange(config.Modules);

                        // Initialize status for all configured modules
                        foreach (var moduleConfig in config.Modules)
                        {
                            _moduleStatuses[moduleConfig.Name] = new ModuleLoadStatus
                            {
                                ModuleName = moduleConfig.Name,
                                State = ModuleState.NotLoaded,
                                IsCore = moduleConfig.LoadOrder < 100, // Consider modules with order < 100 as core
                                Priority = moduleConfig.LoadOrder
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading module configurations");
            }
        }

        public async Task<IModule?> LoadModuleOnDemandAsync(string moduleName)
        {
            _performanceMonitor?.RecordModuleAccess(moduleName);
            // Check if already loaded
            var existingModule = _moduleRegistry.GetModule(moduleName);
            if (existingModule != null)
            {
                _logger.LogDebug("Module {Module} already loaded", moduleName);
                UpdateLastAccessTime(moduleName);
                return existingModule;
            }

            // Get or create semaphore for this module to prevent concurrent loading
            var semaphore = _loadingSemaphores.GetOrAdd(moduleName, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring semaphore
                existingModule = _moduleRegistry.GetModule(moduleName);
                if (existingModule != null)
                {
                    UpdateLastAccessTime(moduleName);
                    return existingModule;
                }

                // Update status to loading
                UpdateModuleStatus(moduleName, ModuleState.Loading);

                // Find module configuration
                var moduleConfig = _moduleConfigs.FirstOrDefault(m => m.Name == moduleName);
                if (moduleConfig == null)
                {
                    _logger.LogWarning("Module configuration not found for {Module}", moduleName);
                    UpdateModuleStatus(moduleName, ModuleState.Error, "Configuration not found");
                    return null;
                }

                // Check dependencies first
                if (moduleConfig.Dependencies?.Any() == true)
                {
                    _logger.LogInformation("Loading dependencies for module {Module}", moduleName);
                    foreach (var dependency in moduleConfig.Dependencies)
                    {
                        var depModule = await LoadModuleOnDemandAsync(dependency);
                        if (depModule == null)
                        {
                            _logger.LogError("Failed to load dependency {Dependency} for module {Module}",
                                dependency, moduleName);
                            UpdateModuleStatus(moduleName, ModuleState.Error, $"Dependency {dependency} failed to load");
                            return null;
                        }
                    }
                }

                // Load the module
                var assemblyPath = Path.Combine(_modulesPath, moduleConfig.AssemblyName);
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogError("Module assembly not found: {Path}", assemblyPath);
                    UpdateModuleStatus(moduleName, ModuleState.Error, "Assembly file not found");
                    return null;
                }

                _logger.LogInformation("Loading module {Module} from {Path}", moduleName, assemblyPath);
                var module = await _moduleLoader.LoadModuleAsync(assemblyPath);

                if (module != null)
                {
                    UpdateModuleStatus(moduleName, ModuleState.Loaded);
                    UpdateLastAccessTime(moduleName);

                    // Cache module metadata
                    _cache.Set($"module_metadata_{moduleName}", new ModuleMetadata
                    {
                        Name = module.Name,
                        Version = module.Version,
                        LoadedAt = DateTime.UtcNow,
                        AssemblyPath = assemblyPath
                    }, TimeSpan.FromHours(1));

                    _logger.LogInformation("Module {Module} loaded successfully", moduleName);
                }
                else
                {
                    UpdateModuleStatus(moduleName, ModuleState.Error, "Failed to load module");
                }

                return module;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading module {Module}", moduleName);
                UpdateModuleStatus(moduleName, ModuleState.Error, ex.Message);
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task PreloadModulesAsync(params string[] moduleNames)
        {
            _logger.LogInformation("Preloading {Count} modules", moduleNames.Length);

            var tasks = moduleNames.Select(name => Task.Run(async () =>
            {
                try
                {
                    await LoadModuleOnDemandAsync(name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error preloading module {Module}", name);
                }
            }));

            await Task.WhenAll(tasks);
        }

        public async Task<bool> IsModuleLoadedAsync(string moduleName)
        {
            return await Task.FromResult(_moduleRegistry.IsModuleRegistered(moduleName));
        }

        public async Task<ModuleLoadStatus> GetModuleStatusAsync(string moduleName)
        {
            return await Task.FromResult(
                _moduleStatuses.TryGetValue(moduleName, out var status)
                    ? status
                    : new ModuleLoadStatus { ModuleName = moduleName, State = ModuleState.NotConfigured }
            );
        }

        public void SetModuleLoadingStrategy(ModuleLoadingStrategy strategy)
        {
            _loadingStrategy = strategy;
            _logger.LogInformation("Module loading strategy set to {Strategy}", strategy);

            if (strategy == ModuleLoadingStrategy.PreloadCore)
            {
                // Preload core modules asynchronously
                Task.Run(async () =>
                {
                    var coreModules = _moduleStatuses
                        .Where(kvp => kvp.Value.IsCore)
                        .Select(kvp => kvp.Key)
                        .ToArray();

                    await PreloadModulesAsync(coreModules);
                });
            }
        }

        public async Task UnloadInactiveModulesAsync(TimeSpan inactiveThreshold)
        {
            var now = DateTime.UtcNow;
            var modulesToUnload = new List<string>();

            foreach (var kvp in _lastAccessTimes)
            {
                if (now - kvp.Value > inactiveThreshold)
                {
                    var status = await GetModuleStatusAsync(kvp.Key);
                    if (!status.IsCore && status.State == ModuleState.Loaded)
                    {
                        modulesToUnload.Add(kvp.Key);
                    }
                }
            }

            foreach (var moduleName in modulesToUnload)
            {
                try
                {
                    _logger.LogInformation("Unloading inactive module {Module}", moduleName);
                    await _moduleLoader.UnloadModuleAsync(moduleName);
                    UpdateModuleStatus(moduleName, ModuleState.NotLoaded);
                    _lastAccessTimes.TryRemove(moduleName, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unloading module {Module}", moduleName);
                }
            }
        }

        public IEnumerable<ModuleLoadStatus> GetAllModuleStatuses()
        {
            return _moduleStatuses.Values.OrderBy(s => s.Priority);
        }

        private void UpdateModuleStatus(string moduleName, ModuleState state, string? error = null)
        {
            _moduleStatuses.AddOrUpdate(moduleName,
                new ModuleLoadStatus
                {
                    ModuleName = moduleName,
                    State = state,
                    LastError = error,
                    LastStateChange = DateTime.UtcNow
                },
                (key, existing) =>
                {
                    existing.State = state;
                    existing.LastError = error;
                    existing.LastStateChange = DateTime.UtcNow;
                    return existing;
                });
        }

        private void UpdateLastAccessTime(string moduleName)
        {
            _lastAccessTimes[moduleName] = DateTime.UtcNow;
        }
    }

    public class ModuleLoadStatus
    {
        public string ModuleName { get; set; } = string.Empty;
        public ModuleState State { get; set; }
        public bool IsCore { get; set; }
        public int Priority { get; set; }
        public string? LastError { get; set; }
        public DateTime LastStateChange { get; set; }
        public DateTime? LastAccessTime { get; set; }
    }

    public enum ModuleState
    {
        NotConfigured,
        NotLoaded,
        Loading,
        Loaded,
        Unloading,
        Error
    }

    public enum ModuleLoadingStrategy
    {
        OnDemand,       // Load modules only when accessed
        PreloadCore,    // Preload core modules, lazy load others
        PreloadAll      // Preload all modules at startup
    }

    public class ModuleMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
    }
}