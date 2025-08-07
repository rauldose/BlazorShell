using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Security;

public class PageAuthorizationService : IPageAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<PageAuthorizationService> _logger;

    public PageAuthorizationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<PageAuthorizationService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<bool> CanAccessPageAsync(string userId, int pageId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            if (await _userManager.IsInRoleAsync(user, "Administrator"))
                return true;

            var page = await _dbContext.NavigationItems
                .FirstOrDefaultAsync(n => n.Id == pageId && n.IsVisible);

            if (page == null)
                return false;

            var hasPermissions = await _dbContext.PagePermissions
                .AnyAsync(p => p.NavigationItemId == pageId);

            if (!hasPermissions)
                return true;

            var userPermission = await _dbContext.PagePermissions
                .AnyAsync(p => p.NavigationItemId == pageId &&
                               p.UserId == userId &&
                               p.IsGranted);

            if (userPermission)
                return true;

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var rolePermission = await _dbContext.PagePermissions
                        .AnyAsync(p => p.NavigationItemId == pageId &&
                                      p.RoleId == role.Id &&
                                      p.IsGranted);
                    if (rolePermission)
                        return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking page access for user {UserId} and page {PageId}",
                userId, pageId);
            return false;
        }
    }

    public async Task GrantPermissionAsync(string userId, int pageId, PermissionType permission)
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
        _logger.LogInformation("Granted {Permission} permission to user {UserId} for page {PageId}",
            permission, userId, pageId);
    }

    public async Task RevokePermissionAsync(string userId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                      p.UserId == userId &&
                                      p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Revoked {Permission} permission from user {UserId} for page {PageId}",
                permission, userId, pageId);
        }
    }

    public async Task<IEnumerable<PagePermission>> GetUserPermissionsAsync(string userId)
    {
        return await _dbContext.PagePermissions
            .Include(p => p.NavigationItem)
            .Where(p => p.UserId == userId && p.IsGranted)
            .ToListAsync();
    }

    public async Task GrantRolePermissionAsync(string roleId, int pageId, PermissionType permission)
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
        _logger.LogInformation("Granted {Permission} permission to role {RoleId} for page {PageId}",
            permission, roleId, pageId);
    }

    public async Task RevokeRolePermissionAsync(string roleId, int pageId, PermissionType permission)
    {
        var existing = await _dbContext.PagePermissions
            .FirstOrDefaultAsync(p => p.NavigationItemId == pageId &&
                                      p.RoleId == roleId &&
                                      p.PermissionType == permission.ToString());

        if (existing != null)
        {
            existing.IsGranted = false;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Revoked {Permission} permission from role {RoleId} for page {PageId}",
                permission, roleId, pageId);
        }
    }

    public async Task<IEnumerable<PagePermission>> GetRolePermissionsAsync(string roleId)
    {
        return await _dbContext.PagePermissions
            .Include(p => p.NavigationItem)
            .Where(p => p.RoleId == roleId && p.IsGranted)
            .ToListAsync();
    }
}
