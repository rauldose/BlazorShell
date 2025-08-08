using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Application.Services;

public class NavigationService : INavigationService
{
    private readonly List<NavigationItem> _navigationItems = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly object _lock = new();

    public event EventHandler? NavigationChanged;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var allItems = await GetNavigationItemsAsync(type);

        if (string.IsNullOrEmpty(userId))
        {
            // Return only public items for anonymous users
            return allItems.Where(item => item.IsPublic);
        }

        var accessibleItems = new List<NavigationItem>();

        foreach (var item in allItems)
        {
            if (await CanAccessNavigationItemAsync(item, userId))
            {
                accessibleItems.Add(item);
            }
        }

        return accessibleItems;
    }

    public async Task<bool> CanAccessNavigationItemAsync(NavigationItem item, string userId)
    {
        if (item == null) return false;

        // Public items are always accessible
        if (item.IsPublic)
            return item.IsVisible;

        // If no user ID, deny access to non-public items
        if (string.IsNullOrEmpty(userId))
            return false;

        using var scope = _serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetService<IPageAuthorizationService>();
        var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();

        if (authService == null || userManager == null)
            return false;

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Check if user is Administrator (bypass all checks)
            if (await userManager.IsInRoleAsync(user, "Administrator"))
                return item.IsVisible;

            // Check minimum role requirement
            if (!string.IsNullOrEmpty(item.MinimumRole))
            {
                if (!await userManager.IsInRoleAsync(user, item.MinimumRole))
                    return false;
            }

            // Check page-specific permissions
            if (!await authService.CanAccessPageAsync(userId, item.Id, PermissionType.View))
                return false;

            return item.IsVisible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking navigation access for user {UserId} and item {ItemName}",
                userId, item.Name);
            return false;
        }
    }

    public void RegisterNavigationItems(IEnumerable<NavigationItem> items)
    {
        lock (_lock)
        {
            foreach (var item in items)
            {
                // Update existing or add new
                var existing = _navigationItems.FirstOrDefault(n => n.Name == item.Name);
                if (existing != null)
                {
                    // Update existing item
                    existing.Id = item.Id;
                    existing.ModuleId = item.ModuleId;
                    existing.DisplayName = item.DisplayName;
                    existing.Url = item.Url;
                    existing.Icon = item.Icon;
                    existing.Order = item.Order;
                    existing.Type = item.Type;
                    existing.IsVisible = item.IsVisible;
                    existing.IsPublic = item.IsPublic;
                    existing.MinimumRole = item.MinimumRole;
                    existing.ParentId = item.ParentId;
                    existing.Module = item.Module;
                    _logger.LogDebug("Updated navigation item: {Name}", item.Name);
                }
                else
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

    public void ClearNavigationItems()
    {
        lock (_lock)
        {
            _navigationItems.Clear();
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}