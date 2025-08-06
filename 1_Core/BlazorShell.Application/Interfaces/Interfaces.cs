using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using BlazorShell.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorShell.Application.Interfaces
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
    /// Module metadata for discovery
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleMetadataAttribute : Attribute
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
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
        public object OldValue { get; }
        public object NewValue { get; }

        public StateChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
