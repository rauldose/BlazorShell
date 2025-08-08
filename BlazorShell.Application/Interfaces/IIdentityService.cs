using BlazorShell.Application.Models;
using Microsoft.AspNetCore.Identity;

namespace BlazorShell.Application.Interfaces;

public interface IIdentityService
{
    Task<UserDto?> GetUserAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<SignInResult> SignInAsync(string email, string password);
}
