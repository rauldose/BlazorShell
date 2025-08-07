using BlazorShell.Core.Entities;
using BlazorShell.Core.Enums;

namespace BlazorShell.Core.Interfaces
{
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
}