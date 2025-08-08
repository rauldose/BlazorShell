using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Models;
using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BlazorShell.Infrastructure.Services.Implementations;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;
        return new UserDto { Id = user.Id, UserName = user.UserName, Email = user.Email };
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<SignInResult> SignInAsync(string email, string password)
    {
        return await _signInManager.PasswordSignInAsync(email, password, false, false);
    }
}
