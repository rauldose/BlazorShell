// Infrastructure/Services/ModuleRegistry.cs - Ensure it maintains state as singleton
using BlazorShell.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlazorShell.Infrastructure.Services
{
    public class ModuleRegistry : IModuleRegistry
    {
        // Use thread-safe collections
        private readonly ConcurrentDictionary<string, IModule> _modules;
        private readonly ILogger<ModuleRegistry> _logger;

        public event EventHandler<ModuleEventArgs>? ModuleRegistered;
        public event EventHandler<ModuleEventArgs>? ModuleUnregistered;

        public ModuleRegistry(ILogger<ModuleRegistry> logger)
        {
            _logger = logger;
            _modules = new ConcurrentDictionary<string, IModule>(StringComparer.OrdinalIgnoreCase);
        }

        public void RegisterModule(IModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (_modules.TryAdd(module.Name, module))
            {
                _logger.LogInformation("Module {Module} registered successfully", module.Name);
                ModuleRegistered?.Invoke(this, new ModuleEventArgs(module, "Registered"));
            }
            else
            {
                _logger.LogWarning("Module {Module} is already registered", module.Name);
            }
        }

        public void UnregisterModule(string moduleName)
        {
            if (_modules.TryRemove(moduleName, out var module))
            {
                _logger.LogInformation("Module {Module} unregistered", moduleName);
                ModuleUnregistered?.Invoke(this, new ModuleEventArgs(module, "Unregistered"));
            }
            else
            {
                _logger.LogWarning("Module {Module} not found for unregistration", moduleName);
            }
        }

        public IModule? GetModule(string moduleName)
        {
            _modules.TryGetValue(moduleName, out var module);
            return module;
        }

        public IEnumerable<IModule> GetModules()
        {
            return _modules.Values.ToList();
        }

        public IEnumerable<IModule> GetModulesByCategory(string category)
        {
            return _modules.Values
                .Where(m => string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public bool IsModuleRegistered(string moduleName)
        {
            return _modules.ContainsKey(moduleName);
        }
    }
}