using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazorShell.Core.Services;

public interface IAuthenticationService
{
    Task<bool> LoginAsync(string email, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<ClaimsPrincipal> GetCurrentUserAsync();
    Task<string?> GetCurrentUserIdAsync();
    Task<string?> GetCurrentUserNameAsync();
    Task<IList<string>> GetCurrentUserRolesAsync();
    Task<bool> IsInRoleAsync(string role);
    Task<bool> HasPermissionAsync(string permission);
    event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;
}
