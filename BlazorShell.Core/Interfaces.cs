using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using BlazorShell.Core.Entities;

namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Base interface that all modules must implement
    /// </summary>
    public interface IModule
    {
        string Name { get; }
        string DisplayName { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }
        string Icon { get; }
        string Category { get; }
        int Order { get; }

        Task<bool> InitializeAsync(IServiceProvider serviceProvider);
        Task<bool> ActivateAsync();
        Task<bool> DeactivateAsync();
        IEnumerable<NavigationItem> GetNavigationItems();
        IEnumerable<Type> GetComponentTypes();
        Dictionary<string, object> GetDefaultSettings();
    }

    /// <summary>
    /// Interface for modules that provide services
    /// </summary>
    public interface IServiceModule : IModule
    {
        void RegisterServices(IServiceCollection services);
    }

    /// <summary>
    /// Interface for modules with configuration requirements
    /// </summary>
    public interface IConfigurableModule : IModule
    {
        Type ConfigurationComponentType { get; }
        Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration);
        Task ApplyConfigurationAsync(Dictionary<string, object> configuration);
    }

    /// <summary>
    /// Module loader responsible for discovering and loading modules
    /// </summary>
    public interface IModuleLoader
    {
        Task InitializeModulesAsync();
        Task<IModule> LoadModuleAsync(string assemblyPath);
        Task<bool> UnloadModuleAsync(string moduleName);
        Task<IEnumerable<IModule>> GetLoadedModulesAsync();
        Task<IModule> GetModuleAsync(string moduleName);
        Task ReloadModuleAsync(string moduleName);
    }

    /// <summary>
    /// Registry for managing loaded modules
    /// </summary>
    public interface IModuleRegistry
    {
        void RegisterModule(IModule module);
        void UnregisterModule(string moduleName);
        IModule GetModule(string moduleName);
        IEnumerable<IModule> GetModules();
        IEnumerable<IModule> GetModulesByCategory(string category);
        bool IsModuleRegistered(string moduleName);
        event EventHandler<ModuleEventArgs> ModuleRegistered;
        event EventHandler<ModuleEventArgs> ModuleUnregistered;
    }

    /// <summary>
    /// Service for managing navigation items
    /// </summary>
    public interface INavigationService
    {
        Task<IEnumerable<NavigationItem>> GetNavigationItemsAsync(NavigationType type);
        Task<IEnumerable<NavigationItem>> GetUserNavigationItemsAsync(string userId, NavigationType type);
        Task<bool> CanAccessNavigationItemAsync(NavigationItem item, string userId);
        void RegisterNavigationItems(IEnumerable<NavigationItem> items);
        void UnregisterNavigationItems(string moduleName);
        event EventHandler NavigationChanged;
    }

    /// <summary>
    /// Module authorization service
    /// </summary>
    public interface IModuleAuthorizationService
    {
        Task<bool> CanAccessModuleAsync(string userId, string moduleName);
        Task<bool> HasPermissionAsync(string userId, string moduleName, PermissionType permission);
        Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission);
        Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission);
        Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId);
    }

    /// <summary>
    /// State container for managing application state
    /// </summary>
    public interface IStateContainer
    {
        T GetState<T>(string key) where T : class;
        void SetState<T>(string key, T value) where T : class;
        bool RemoveState(string key);
        void ClearState();
        event EventHandler<StateChangedEventArgs> StateChanged;
    }

    /// <summary>
    /// Plugin assembly loader
    /// </summary>
    public interface IPluginAssemblyLoader
    {
        Assembly LoadPlugin(string path);
        void UnloadPlugin(string pluginName);
        IEnumerable<Type> GetTypesFromAssembly(Assembly assembly, Type interfaceType);
        T CreateInstance<T>(Type type) where T : class;
    }

    /// <summary>
    /// Dynamic route service
    /// </summary>
    public interface IDynamicRouteService
    {
        void RegisterRoutes(IEnumerable<RouteInfo> routes);
        void UnregisterRoutes(string moduleName);
        IEnumerable<RouteInfo> GetRoutes();
        RouteInfo? GetRoute(string path);
    }

    /// <summary>
    /// Lazy module loader
    /// </summary>
    public interface ILazyModuleLoader
    {
        Task LoadModuleOnDemandAsync(string moduleName);
        Task<bool> IsModuleLoadedAsync(string moduleName);
        void SetModuleLoadingStrategy(ModuleLoadingStrategy strategy);
        Dictionary<string, ModuleLoadStatus> GetAllModuleStatuses();
    }

    /// <summary>
    /// Module hot reload service
    /// </summary>
    public interface IModuleHotReloadService
    {
        Task StartWatchingAsync(string moduleName, string assemblyPath);
        Task StopWatchingAsync(string moduleName);
        event EventHandler<ModuleReloadEventArgs> ModuleReloaded;
    }

    /// <summary>
    /// Module performance monitoring
    /// </summary>
    public interface IModulePerformanceMonitor
    {
        void RecordModuleLoadTime(string moduleName, TimeSpan loadTime);
        void RecordModuleMemoryUsage(string moduleName, long memoryUsage);
        Task<ModulePerformanceData> GetModulePerformanceAsync(string moduleName);
        Task<IEnumerable<ModulePerformanceData>> GetAllModulePerformanceAsync();
    }

    /// <summary>
    /// Module service provider for dependency injection
    /// </summary>
    public interface IModuleServiceProvider
    {
        IServiceProvider CreateServiceProvider(IModule module);
        T GetService<T>(string moduleName) where T : class;
        void RegisterModuleServices(string moduleName, Action<IServiceCollection> configureServices);
    }

    /// <summary>
    /// Infrastructure services
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendTemplateEmailAsync(string to, string templateName, object model);
    }

    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string? folder = null);
        Task<Stream> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }

    /// <summary>
    /// Route information
    /// </summary>
    public class RouteInfo
    {
        public string Path { get; set; } = string.Empty;
        public Type ComponentType { get; set; } = null!;
        public string ModuleName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Module loading strategy
    /// </summary>
    public enum ModuleLoadingStrategy
    {
        PreloadAll,
        PreloadCore,
        OnDemand
    }

    /// <summary>
    /// Module load status
    /// </summary>
    public enum ModuleLoadStatus
    {
        NotLoaded,
        Loading,
        Loaded,
        Failed
    }

    /// <summary>
    /// Module performance data
    /// </summary>
    public class ModulePerformanceData
    {
        public string ModuleName { get; set; } = string.Empty;
        public TimeSpan LoadTime { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Module metadata for discovery
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleMetadataAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    /// <summary>
    /// Event args for module events
    /// </summary>
    public class ModuleEventArgs : EventArgs
    {
        public IModule Module { get; }
        public string Action { get; }
        public DateTime Timestamp { get; }

        public ModuleEventArgs(IModule module, string action)
        {
            Module = module;
            Action = action;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event args for state changes
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public StateChangedEventArgs(string key, object? oldValue, object? newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Event args for module reload
    /// </summary>
    public class ModuleReloadEventArgs : EventArgs
    {
        public string ModuleName { get; }
        public bool Success { get; }
        public Exception? Exception { get; }

        public ModuleReloadEventArgs(string moduleName, bool success, Exception? exception = null)
        {
            ModuleName = moduleName;
            Success = success;
            Exception = exception;
        }
    }
}