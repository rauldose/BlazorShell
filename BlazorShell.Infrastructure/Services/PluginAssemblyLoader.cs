using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Application.Interfaces;

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
    /// State Container implementation
    /// </summary>
    public class StateContainer : IStateContainer, IDisposable
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

        public void Dispose()
        {
            StateChanged = null;
        }
    }
}