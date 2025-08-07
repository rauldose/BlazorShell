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
}