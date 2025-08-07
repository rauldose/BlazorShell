using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces;

public interface INavigationService
{
    Task<IEnumerable<NavigationItem>> GetNavigationItemsAsync(NavigationType type);
    Task<IEnumerable<NavigationItem>> GetUserNavigationItemsAsync(string userId, NavigationType type);
    Task<bool> CanAccessNavigationItemAsync(NavigationItem item, string userId);
    void RegisterNavigationItems(IEnumerable<NavigationItem> items);
    void UnregisterNavigationItems(string moduleName);
    event EventHandler NavigationChanged;
}

