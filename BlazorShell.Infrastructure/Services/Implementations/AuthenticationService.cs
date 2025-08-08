using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using BlazorShell.Application.Events;
using BlazorShell.Application.Services;

namespace BlazorShell.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    public AuthenticationService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", email);

                var user = await _userManager.FindByEmailAsync(email);
                OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs
                {
                    IsAuthenticated = true,
                    UserName = user?.UserName,
                    UserId = user?.Id
                });

                return true;
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", email);
            }
            else if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("User {Email} requires two-factor authentication", email);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", email);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userName = authState.User.Identity?.Name;

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserName} logged out", userName);

            OnAuthenticationStateChanged(new AuthenticationStateChangedEventArgs
            {
                IsAuthenticated = false,
                UserName = null,
                UserId = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    public async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var user = await GetCurrentUserAsync();
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetCurrentUserNameAsync()
    {
        var user = await GetCurrentUserAsync();
        return user.Identity?.Name;
    }

    public async Task<IList<string>> GetCurrentUserRolesAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return new List<string>();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new List<string>();

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var user = await GetCurrentUserAsync();
        return user.IsInRole(role);
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        // This would integrate with your existing permission system
        // For now, just check if user is authenticated
        return await IsAuthenticatedAsync();
    }

    protected virtual void OnAuthenticationStateChanged(AuthenticationStateChangedEventArgs e)
    {
        AuthenticationStateChanged?.Invoke(this, e);
    }
}
