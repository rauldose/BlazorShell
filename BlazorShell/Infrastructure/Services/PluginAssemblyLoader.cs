using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using BlazorShell.Core.Enums;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Plugin assembly loader with isolation using AssemblyLoadContext
    /// </summary>
    public class PluginAssemblyLoader : IPluginAssemblyLoader
    {
        private readonly ILogger<PluginAssemblyLoader> _logger;
        private readonly Dictionary<string, PluginLoadContext> _loadContexts;
        private readonly IServiceProvider _serviceProvider;

        public PluginAssemblyLoader(ILogger<PluginAssemblyLoader> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _loadContexts = new Dictionary<string, PluginLoadContext>();
        }

        public Assembly LoadPlugin(string path)
        {
            try
            {
                _logger.LogDebug("Loading plugin assembly from {Path}", path);

                var fileName = Path.GetFileNameWithoutExtension(path);

                // Check if already loaded
                if (_loadContexts.ContainsKey(fileName))
                {
                    _logger.LogWarning("Plugin {Plugin} is already loaded", fileName);
                    return _loadContexts[fileName].LoadedAssembly;
                }

                // Create isolated load context
                var loadContext = new PluginLoadContext(path);

                // Load the assembly
                var assembly = loadContext.LoadFromAssemblyPath(path);

                // Store context for later unloading
                _loadContexts[fileName] = loadContext;
                loadContext.LoadedAssembly = assembly;

                _logger.LogInformation("Successfully loaded plugin {Plugin} from {Path}",
                    assembly.GetName().Name, path);

                return assembly;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {Path}", path);
                throw;
            }
        }

        public void UnloadPlugin(string pluginName)
        {
            try
            {
                if (_loadContexts.TryGetValue(pluginName, out var context))
                {
                    _logger.LogDebug("Unloading plugin {Plugin}", pluginName);

                    // Request unload - actual unload happens asynchronously
                    context.Unload();
                    _loadContexts.Remove(pluginName);

                    // Force garbage collection to help with unloading
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    _logger.LogInformation("Plugin {Plugin} unloaded", pluginName);
                }
                else
                {
                    _logger.LogWarning("Plugin {Plugin} not found in loaded contexts", pluginName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin {Plugin}", pluginName);
            }
        }

        public IEnumerable<Type> GetTypesFromAssembly(Assembly assembly, Type interfaceType)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t));
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogError(ex, "Error loading types from assembly {Assembly}", assembly.FullName);

                // Return types that were successfully loaded
                return ex.Types.Where(t => t != null && interfaceType.IsAssignableFrom(t));
            }
        }

        public T CreateInstance<T>(Type type) where T : class
        {
            try
            {
                // Try to create instance using DI container first
                var instance = ActivatorUtilities.CreateInstance(_serviceProvider, type) as T;

                if (instance == null)
                {
                    // Fallback to direct activation
                    instance = Activator.CreateInstance(type) as T;
                }

                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create instance of type {Type}", type.FullName);
                throw;
            }
        }

        /// <summary>
        /// Custom AssemblyLoadContext for plugin isolation
        /// </summary>
        private class PluginLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public Assembly LoadedAssembly { get; set; }

            public PluginLoadContext(string pluginPath) : base(isCollectible: true)
            {
                _resolver = new AssemblyDependencyResolver(pluginPath);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                // Try to resolve assembly from plugin directory
                string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                // Let the default context handle it (for system assemblies)
                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }

                return IntPtr.Zero;
            }
        }
    }


    /// <summary>
    /// Navigation Service implementation
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly List<NavigationItem> _navigationItems;
        private readonly ILogger<NavigationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly object _lock = new object();

        public event EventHandler NavigationChanged;

        public NavigationService(ILogger<NavigationService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _navigationItems = new List<NavigationItem>();
        }

        public async Task<IEnumerable<NavigationItem>> GetNavigationItemsAsync(NavigationType type)
        {
            lock (_lock)
            {
                return _navigationItems
                    .Where(n => n.Type == type || n.Type == NavigationType.Both)
                    .OrderBy(n => n.Order)
                    .ToList();
            }
        }

        public async Task<IEnumerable<NavigationItem>> GetUserNavigationItemsAsync(string userId, NavigationType type)
        {
            var authService = _serviceProvider.GetService<IModuleAuthorizationService>();
            var allItems = await GetNavigationItemsAsync(type);

            if (authService == null)
            {
                // If no auth service, return public items only
                return allItems.Where(item => string.IsNullOrEmpty(item.RequiredPermission) &&
                                             string.IsNullOrEmpty(item.RequiredRole));
            }

            var userItems = new List<NavigationItem>();

            foreach (var item in allItems)
            {
                if (await CanAccessNavigationItemAsync(item, userId))
                {
                    userItems.Add(item);
                }
            }

            return userItems;
        }

        public async Task<bool> CanAccessNavigationItemAsync(NavigationItem item, string userId)
        {
            if (item == null) return false;

            // If no requirements, it's public
            if (string.IsNullOrEmpty(item.RequiredPermission) && string.IsNullOrEmpty(item.RequiredRole))
                return item.IsVisible;

            // Check if item requires authentication
            if (!string.IsNullOrEmpty(item.RequiredPermission) || !string.IsNullOrEmpty(item.RequiredRole))
            {
                if (string.IsNullOrEmpty(userId))
                    return false;

                var authService = _serviceProvider.GetService<IModuleAuthorizationService>();
                if (authService == null)
                    return false;

                // Check permission
                if (!string.IsNullOrEmpty(item.RequiredPermission))
                {
                    var parts = item.RequiredPermission.Split('.');
                    if (parts.Length == 2)
                    {
                        var moduleName = parts[0];
                        if (Enum.TryParse<PermissionType>(parts[1], out var permission))
                        {
                            if (!await authService.HasPermissionAsync(userId, moduleName, permission))
                                return false;
                        }
                    }
                }

                // Check role
                if (!string.IsNullOrEmpty(item.RequiredRole))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
                    if (userManager != null)
                    {
                        var user = await userManager.FindByIdAsync(userId);
                        if (user == null || !await userManager.IsInRoleAsync(user, item.RequiredRole))
                            return false;
                    }
                }
            }

            return item.IsVisible;
        }

        public void RegisterNavigationItems(IEnumerable<NavigationItem> items)
        {
            lock (_lock)
            {
                foreach (var item in items)
                {
                    if (!_navigationItems.Any(n => n.Name == item.Name))
                    {
                        _navigationItems.Add(item);
                        _logger.LogDebug("Registered navigation item: {Name}", item.Name);
                    }
                }

                NavigationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UnregisterNavigationItems(string moduleName)
        {
            lock (_lock)
            {
                var itemsToRemove = _navigationItems
                    .Where(n => n.Module?.Name == moduleName)
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    _navigationItems.Remove(item);
                    _logger.LogDebug("Unregistered navigation item: {Name}", item.Name);
                }

                if (itemsToRemove.Any())
                {
                    NavigationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// State Container implementation
    /// </summary>
    public class StateContainer : IStateContainer
    {
        private readonly Dictionary<string, object> _state;
        private readonly object _lock = new object();
        private readonly ILogger<StateContainer> _logger;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public StateContainer(ILogger<StateContainer> logger)
        {
            _logger = logger;
            _state = new Dictionary<string, object>();
        }

        public T GetState<T>(string key) where T : class
        {
            lock (_lock)
            {
                if (_state.TryGetValue(key, out var value))
                {
                    return value as T;
                }
                return null;
            }
        }

        public void SetState<T>(string key, T value) where T : class
        {
            lock (_lock)
            {
                var oldValue = _state.ContainsKey(key) ? _state[key] : null;
                _state[key] = value;

                _logger.LogDebug("State updated for key: {Key}", key);
                StateChanged?.Invoke(this, new StateChangedEventArgs(key, oldValue, value));
            }
        }

        public bool RemoveState(string key)
        {
            lock (_lock)
            {
                if (_state.TryGetValue(key, out var oldValue))
                {
                    _state.Remove(key);
                    _logger.LogDebug("State removed for key: {Key}", key);
                    StateChanged?.Invoke(this, new StateChangedEventArgs(key, oldValue, null));
                    return true;
                }
                return false;
            }
        }

        public void ClearState()
        {
            lock (_lock)
            {
                _state.Clear();
                _logger.LogDebug("State cleared");
                StateChanged?.Invoke(this, new StateChangedEventArgs(null, null, null));
            }
        }
    }
}