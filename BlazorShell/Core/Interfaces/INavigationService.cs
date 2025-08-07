using BlazorShell.Core.Entities;
using BlazorShell.Core.Enums;

namespace BlazorShell.Core.Interfaces
{
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
}