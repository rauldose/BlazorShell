using BlazorShell.Application.Models;
using Microsoft.AspNetCore.Identity;

namespace BlazorShell.Application.Interfaces;

public interface IIdentityService
{
    Task<UserDto?> FindByIdAsync(string userId);
    Task<IList<string>> GetRolesAsync(string userId);
    Task<SignInResult> SignInAsync(string email, string password);
    Task<IEnumerable<UserDto>> GetUsersAsync(int take);
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<RoleDto?> FindRoleByIdAsync(string roleId);
}
