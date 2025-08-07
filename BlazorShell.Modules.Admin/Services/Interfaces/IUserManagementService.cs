// BlazorShell.Modules.Admin/Services/Interfaces/IUserManagementService.cs
using BlazorShell.Core.Entities;
using System.Collections.Generic;

namespace BlazorShell.Modules.Admin.Services;

public interface IUserManagementService
{
    Task<IEnumerable<UserInfo>> GetUsersAsync(int page = 1, int pageSize = 20);
    Task<UserInfo?> GetUserAsync(string userId);
    Task<UserOperationResult> CreateUserAsync(CreateUserModel model);
    Task<UserOperationResult> UpdateUserAsync(string userId, UpdateUserModel model);
    Task<UserOperationResult> DeleteUserAsync(string userId);
    Task<UserOperationResult> ToggleUserStatusAsync(string userId);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);
    Task<UserOperationResult> UpdateUserRolesAsync(string userId, List<string> roles);
}

