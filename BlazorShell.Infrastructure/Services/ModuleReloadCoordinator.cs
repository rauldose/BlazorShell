using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorShell.Application.Interfaces;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Module reload coordinator that handles safe module reloading without breaking active components
    /// </summary>
    public class ModuleReloadCoordinator : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModuleReloadCoordinator> _logger;
        private readonly SemaphoreSlim _reloadLock = new(1, 1);
        private readonly Dictionary<string, ModuleReloadContext> _reloadContexts = new();

        public event EventHandler<ModuleReloadEventArgs>? ModuleReloading;
        public event EventHandler<ModuleReloadEventArgs>? ModuleReloaded;

        public ModuleReloadCoordinator(
            IServiceProvider serviceProvider,
            ILogger<ModuleReloadCoordinator> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Safely reload a module with context preservation
        /// </summary>
        public async Task<bool> SafeReloadModuleAsync(string moduleName)
        {
            await _reloadLock.WaitAsync();
            try
            {
                _logger.LogInformation("Starting safe reload of module {Module}", moduleName);

                // Notify listeners that reload is starting
                ModuleReloading?.Invoke(this, new ModuleReloadEventArgs { ModuleName = moduleName });

                // Create reload context
                var context = new ModuleReloadContext
                {
                    ModuleName = moduleName,
                    StartTime = DateTime.UtcNow
                };

                _reloadContexts[moduleName] = context;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var moduleLoader = scope.ServiceProvider.GetRequiredService<IModuleLoader>();
                    var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();

                    // Check if module is currently loaded
                    var currentModule = moduleRegistry.GetModule(moduleName);
                    if (currentModule == null)
                    {
                        _logger.LogWarning("Module {Module} is not loaded, cannot reload", moduleName);
                        return false;
                    }

                    // Store module state before unload
                    context.ModuleState = await CaptureModuleStateAsync(currentModule);

                    // Perform the reload
                    await moduleLoader.ReloadModuleAsync(moduleName);

                    // Wait for module to stabilize
                    await Task.Delay(500);

                    // Restore module state if needed
                    var reloadedModule = moduleRegistry.GetModule(moduleName);
                    if (reloadedModule != null && context.ModuleState != null)
                    {
                        await RestoreModuleStateAsync(reloadedModule, context.ModuleState);
                    }

                    context.EndTime = DateTime.UtcNow;
                    context.Success = true;

                    // Notify listeners that reload is complete
                    ModuleReloaded?.Invoke(this, new ModuleReloadEventArgs
                    {
                        ModuleName = moduleName,
                        Success = true
                    });

                    _logger.LogInformation("Module {Module} reloaded successfully in {Duration}ms",
                        moduleName,
                        (context.EndTime.Value - context.StartTime).TotalMilliseconds);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during safe reload of module {Module}", moduleName);

                ModuleReloaded?.Invoke(this, new ModuleReloadEventArgs
                {
                    ModuleName = moduleName,
                    Success = false,
                    Error = ex.Message
                });

                return false;
            }
            finally
            {
                _reloadLock.Release();
            }
        }

        /// <summary>
        /// Check if a module can be safely reloaded
        /// </summary>
        public async Task<ModuleReloadStatus> CanReloadModuleAsync(string moduleName)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
                var module = moduleRegistry.GetModule(moduleName);

                if (module == null)
                {
                    return new ModuleReloadStatus
                    {
                        CanReload = false,
                        Reason = "Module is not loaded"
                    };
                }

                // Check if module is core/system module
                if (IsSystemModule(moduleName))
                {
                    return new ModuleReloadStatus
                    {
                        CanReload = false,
                        Reason = "System modules cannot be reloaded"
                    };
                }

                // Check if module has active connections
                if (await HasActiveConnectionsAsync(moduleName))
                {
                    return new ModuleReloadStatus
                    {
                        CanReload = false,
                        Reason = "Module has active connections"
                    };
                }

                return new ModuleReloadStatus
                {
                    CanReload = true
                };
            }
        }

        private async Task<Dictionary<string, object>?> CaptureModuleStateAsync(IModule module)
        {
            try
            {
                var state = new Dictionary<string, object>();

                // Capture basic module info
                state["Name"] = module.Name;
                state["Version"] = module.Version;
                state["IsActive"] = true;

                // If module implements IStatefulModule, capture its state
                if (module is IStatefulModule statefulModule)
                {
                    state["CustomState"] = await statefulModule.CaptureStateAsync();
                }

                return state;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture state for module {Module}", module.Name);
                return null;
            }
        }

        private async Task RestoreModuleStateAsync(IModule module, Dictionary<string, object> state)
        {
            try
            {
                // If module implements IStatefulModule, restore its state
                if (module is IStatefulModule statefulModule && state.ContainsKey("CustomState"))
                {
                    await statefulModule.RestoreStateAsync(state["CustomState"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore state for module {Module}", module.Name);
            }
        }

        private bool IsSystemModule(string moduleName)
        {
            // Define your system/core modules that shouldn't be reloaded
            var systemModules = new[] { "Core", "System", "Authentication", "Navigation" };
            return systemModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<bool> HasActiveConnectionsAsync(string moduleName)
        {
            // Check if there are active SignalR connections or other persistent connections
            // This is a placeholder - implement based on your specific needs
            await Task.CompletedTask;
            return false;
        }

        public void Dispose()
        {
            _reloadLock?.Dispose();
        }

        private class ModuleReloadContext
        {
            public string ModuleName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public bool Success { get; set; }
            public Dictionary<string, object>? ModuleState { get; set; }
        }
    }

    public class ModuleReloadEventArgs : EventArgs
    {
        public string ModuleName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class ModuleReloadStatus
    {
        public bool CanReload { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Interface for modules that can preserve state across reloads
    /// </summary>
    public interface IStatefulModule
    {
        Task<object> CaptureStateAsync();
        Task RestoreStateAsync(object state);
    }
}