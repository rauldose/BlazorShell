using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Application.Services;

/// <summary>
/// Unified authorization service that combines module and page permissions
/// </summary>
public class UnifiedAuthorizationService : IModuleAuthorizationService, IPageAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedAuthorizationService> _logger;

    private const int CACHE_MINUTES = 5;
    private const string UserVersionKeyFmt = "auth:perms:ver:{0}";

    private string UserVersionKey(string userId) => string.Format(UserVersionKeyFmt, userId);

    private int GetUserVersion(string userId)
        => _cache.GetOrCreate(UserVersionKey(userId), e => 0);

    private void BumpUserVersion(string userId)
    {
        var key = UserVersionKey(userId);
        var cur = _cache.Get<int?>(key) ?? 0;
        _cache.Set(key, cur + 1);
    }

    private string PageKey(string userId, int pageId, PermissionType permission)
    {
        var v = GetUserVersion(userId);
        return $"auth:perms:user:{userId}:v:{v}:page:{pageId}:perm:{permission}";
    }

    public UnifiedAuthorizationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IMemoryCache cache,
        ILogger<UnifiedAuthorizationService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _cache = cache;
        _logger = logger;
    }

    #region IModuleAuthorizationService Implementation

    public async Task<bool> CanAccessModuleAsync(string userId, string moduleName)
    {
        return await HasPermissionAsync(userId, moduleName, PermissionType.View);
    }

    public async Task<bool> HasPermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(moduleName))
            return false;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if user is Administrator (bypass all checks)
            if (await _userManager.IsInRoleAsync(user, "Administrator"))
                return true;

            var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
            if (module == null) return false;

            // Check module's RequiredRole if set
            if (!string.IsNullOrEmpty(module.RequiredRole))
            {
                if (!await _userManager.IsInRoleAsync(user, module.RequiredRole))
                    return false;
            }

            // Check user's direct module permission
            var userPermission = await _dbContext.ModulePermissions
                .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                         p.UserId == userId &&
                                         p.PermissionType == permission.ToString());

            if (userPermission?.IsGranted == true)
                return true;

            // Check role-based module permissions
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var rolePermission = await _dbContext.ModulePermissions
                        .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                                 p.RoleId == role.Id &&
                                                 p.PermissionType == permission.ToString());

                    if (rolePermission?.IsGranted == true)
                        return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking module permission {Permission} for user {UserId} and module {ModuleName}",
                permission, userId, moduleName);
            return false;
        }
    }

    async Task IModuleAuthorizationService.GrantPermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
        if (module == null) throw new InvalidOperationException($"Module {moduleName} not found");

        var existing = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                     p.UserId == userId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.GrantedDate = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ModulePermissions.Add(new ModulePermission
            {
                ModuleId = module.Id,
                UserId = userId,
                PermissionType = permission.ToString(),
                IsGranted = true,
                GrantedDate = DateTime.UtcNow,
                GrantedBy = "System"
            });
        }

        await _dbContext.SaveChangesAsync();
        ClearUserCache(userId);
    }

    async Task IModuleAuthorizationService.RevokePermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
        if (module == null) return;

        var existing = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                     p.UserId == userId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            ClearUserCache(userId);
        }
    }

    async Task<IEnumerable<ModulePermission>> IModuleAuthorizationService.GetUserPermissionsAsync(string userId)
    {
        return await _dbContext.ModulePermissions
            .Include(p => p.Module)
            .Where(p => p.UserId == userId && p.IsGranted)
            .ToListAsync();
    }

    public async Task GrantRolePermissionAsync(string roleId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
        if (module == null) throw new InvalidOperationException($"Module {moduleName} not found");

        var existing = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                     p.RoleId == roleId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.GrantedDate = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ModulePermissions.Add(new ModulePermission
            {
                ModuleId = module.Id,
                RoleId = roleId,
                PermissionType = permission.ToString(),
                IsGranted = true,
                GrantedDate = DateTime.UtcNow,
                GrantedBy = "System"
            });
        }

        await _dbContext.SaveChangesAsync();
        await ClearRoleCacheAsync(roleId);
    }

    public async Task RevokeRolePermissionAsync(string roleId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
        if (module == null) return;

        var existing = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                     p.RoleId == roleId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            await ClearRoleCacheAsync(roleId);
        }
    }

    async Task<IEnumerable<ModulePermission>> IModuleAuthorizationService.GetRolePermissionsAsync(string roleId)
    {
        return await _dbContext.ModulePermissions
            .Include(p => p.Module)
            .Where(p => p.RoleId == roleId && p.IsGranted)
            .ToListAsync();
    }

    #endregion

    #region IPageAuthorizationService Implementation

    public async Task<bool> CanAccessPageAsync(string userId, int pageId, PermissionType permission)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        var cacheKey = PageKey(userId, pageId, permission);
        var cached = _cache.Get<bool?>(cacheKey);
        if (cached.HasValue) return cached.Value;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if user is Administrator
            if (await _userManager.IsInRoleAsync(user, "Administrator"))
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(CACHE_MINUTES));
                return true;
            }

            var navItem = await _dbContext.NavigationItems
                .Include(n => n.Module)
                .FirstOrDefaultAsync(n => n.Id == pageId);

            if (navItem == null) return false;

            // Check if page is public
            if (navItem.IsPublic)
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(CACHE_MINUTES));
                return true;
            }

            // Check minimum role requirement
            if (!string.IsNullOrEmpty(navItem.MinimumRole))
            {
                if (!await _userManager.IsInRoleAsync(user, navItem.MinimumRole))
                {
                    _cache.Set(cacheKey, false, TimeSpan.FromMinutes(CACHE_MINUTES));
                    return false;
                }
            }

            // Check module permission if navigation item belongs to a module
            if (navItem.Module != null)
            {
                var hasModuleAccess = await HasPermissionAsync(userId, navItem.Module.Name!, permission);
                if (!hasModuleAccess)
                {
                    _cache.Set(cacheKey, false, TimeSpan.FromMinutes(CACHE_MINUTES));
                    return false;
                }
            }

            // Check specific page permission
            var hasPagePermission = await CheckPagePermissionAsync(userId, pageId, permission.ToString());

            _cache.Set(cacheKey, hasPagePermission,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_MINUTES)));

            return hasPagePermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking page access for user {UserId} and page {PageId}", userId, pageId);
            return false;
        }
    }

    private async Task<bool> CheckPagePermissionAsync(string userId, int pageId, string permissionType)
    {
        // Check user's direct page permission
        var userPermission = await _dbContext.PagePermissions
            .AnyAsync(p => p.NavigationItemId == pageId &&
                          p.UserId == userId &&
                          p.PermissionType == permissionType &&
                          p.IsGranted);

        if (userPermission) return true;

        // Check role-based page permissions
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var rolePermission = await _dbContext.PagePermissions
                        .AnyAsync(p => p.NavigationItemId == pageId &&
                                      p.RoleId == role.Id &&
                                      p.PermissionType == permissionType &&
                                      p.IsGranted);
                    if (rolePermission) return true;
                }
            }
        }

        return false;
    }

    async Task IPageAuthorizationService.GrantPermissionAsync(string userId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                     p.UserId == userId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.GrantedDate = DateTime.UtcNow;
        }
        else
        {
            _dbContext.PagePermissions.Add(new PagePermission
            {
                NavigationItemId = pageId,
                UserId = userId,
                PermissionType = permission.ToString(),
                IsGranted = true,
                GrantedDate = DateTime.UtcNow,
                GrantedBy = "System"
            });
        }

        await _dbContext.SaveChangesAsync();
        ClearUserCache(userId);
    }

    async Task IPageAuthorizationService.RevokePermissionAsync(string userId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                     p.UserId == userId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            ClearUserCache(userId);
        }
    }

    async Task<IEnumerable<PagePermission>> IPageAuthorizationService.GetUserPermissionsAsync(string userId)
    {
        return await _dbContext.PagePermissions
            .Include(p => p.NavigationItem)
            .Where(p => p.UserId == userId && p.IsGranted)
            .ToListAsync();
    }

    async Task IPageAuthorizationService.GrantRolePermissionAsync(string roleId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                     p.RoleId == roleId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.GrantedDate = DateTime.UtcNow;
        }
        else
        {
            _dbContext.PagePermissions.Add(new PagePermission
            {
                NavigationItemId = pageId,
                RoleId = roleId,
                PermissionType = permission.ToString(),
                IsGranted = true,
                GrantedDate = DateTime.UtcNow,
                GrantedBy = "System"
            });
        }

        await _dbContext.SaveChangesAsync();
        await ClearRoleCacheAsync(roleId);
    }

    async Task IPageAuthorizationService.RevokeRolePermissionAsync(string roleId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                     p.RoleId == roleId &&
                                     p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            await ClearRoleCacheAsync(roleId);
        }
    }

    async Task<IEnumerable<PagePermission>> IPageAuthorizationService.GetRolePermissionsAsync(string roleId)
    {
        return await _dbContext.PagePermissions
            .Include(p => p.NavigationItem)
            .Where(p => p.RoleId == roleId && p.IsGranted)
            .ToListAsync();
    }

    #endregion

    #region Cache Management

    private void ClearUserCache(string userId) => BumpUserVersion(userId);

    private async Task ClearRoleCacheAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return;

        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        foreach (var u in users)
            BumpUserVersion(u.Id);
    }
    #endregion
}