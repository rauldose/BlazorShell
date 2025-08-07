using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces;

public interface IModuleAuthorizationService
{
    Task<bool> CanAccessModuleAsync(string userId, string moduleName);
    Task<bool> HasPermissionAsync(string userId, string moduleName, PermissionType permission);
    Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission);
    Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission);
    Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId);
    Task GrantRolePermissionAsync(string roleId, string moduleName, PermissionType permission);
    Task RevokeRolePermissionAsync(string roleId, string moduleName, PermissionType permission);
    Task<IEnumerable<ModulePermission>> GetRolePermissionsAsync(string roleId);
}

