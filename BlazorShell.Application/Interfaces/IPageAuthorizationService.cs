using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces;

public interface IPageAuthorizationService
{
    Task<bool> CanAccessPageAsync(string userId, int pageId, PermissionType permission);
    Task GrantPermissionAsync(string userId, int pageId, PermissionType permission);
    Task RevokePermissionAsync(string userId, int pageId, PermissionType permission);
    Task<IEnumerable<PagePermission>> GetUserPermissionsAsync(string userId);
    Task GrantRolePermissionAsync(string roleId, int pageId, PermissionType permission);
    Task RevokeRolePermissionAsync(string roleId, int pageId, PermissionType permission);
    Task<IEnumerable<PagePermission>> GetRolePermissionsAsync(string roleId);
}
