using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Security;

public class ModuleAuthorizationService : IModuleAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<ModuleAuthorizationService> _logger;

    public ModuleAuthorizationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<ModuleAuthorizationService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<bool> CanAccessModuleAsync(string userId, string moduleName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            if (await _userManager.IsInRoleAsync(user, "Administrator"))
                return true;

            var module = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName && m.IsEnabled);

            if (module == null)
                return false;

            var userPermission = await _dbContext.ModulePermissions
                .AnyAsync(p => p.ModuleId == module.Id &&
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
                    var rolePermission = await _dbContext.ModulePermissions
                        .AnyAsync(p => p.ModuleId == module.Id &&
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
            _logger.LogError(ex, "Error checking module access for user {UserId} and module {ModuleName}",
                userId, moduleName);
            return false;
        }
    }

    public async Task<bool> HasPermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            if (await _userManager.IsInRoleAsync(user, "Administrator"))
                return true;

            var module = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName && m.IsEnabled);

            if (module == null)
                return false;

            var userPermission = await _dbContext.ModulePermissions
                .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                          p.UserId == userId &&
                                          p.PermissionType == permission.ToString());

            if (userPermission?.IsGranted == true)
                return true;

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
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId} and module {ModuleName}",
                permission, userId, moduleName);
            return false;
        }
    }

    public async Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules
            .FirstOrDefaultAsync(m => m.Name == moduleName);

        if (module == null)
            throw new InvalidOperationException($"Module {moduleName} not found");

        var existingPermission = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                      p.UserId == userId &&
                                      p.PermissionType == permission.ToString());

        if (existingPermission != null)
        {
            existingPermission.IsGranted = true;
            existingPermission.GrantedDate = DateTime.UtcNow;
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
        _logger.LogInformation("Granted {Permission} permission to user {UserId} for module {ModuleName}",
            permission, userId, moduleName);
    }

    public async Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission)
    {
        var module = await _dbContext.Modules
            .FirstOrDefaultAsync(m => m.Name == moduleName);

        if (module == null)
            return;

        var existingPermission = await _dbContext.ModulePermissions
            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                      p.UserId == userId &&
                                      p.PermissionType == permission.ToString());

        if (existingPermission != null)
        {
            existingPermission.IsGranted = false;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Revoked {Permission} permission from user {UserId} for module {ModuleName}",
                permission, userId, moduleName);
        }
    }

    public async Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId)
    {
        return await _dbContext.ModulePermissions
            .Include(p => p.Module)
            .Where(p => p.UserId == userId && p.IsGranted)
            .ToListAsync();
    }
}
