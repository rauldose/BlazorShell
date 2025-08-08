using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Models;
using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    public async Task<UserDto?> FindByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive
        };
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user == null
            ? new List<string>()
            : await _userManager.GetRolesAsync(user);
    }

    public Task<SignInResult> SignInAsync(string email, string password) =>
        _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);

    public async Task<IEnumerable<UserDto>> GetUsersAsync(int take)
    {
        return await _userManager.Users
            .OrderBy(u => u.UserName)
            .Take(take)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName,
                Email = u.Email,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        return await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description
            })
            .ToListAsync();
    }

    public async Task<RoleDto?> FindRoleByIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description
        };
    }
}

