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

    }
}

