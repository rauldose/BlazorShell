using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BlazorShell.Core.Entities;
using BlazorShell.Core.Interfaces;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Server;

namespace BlazorShell.Infrastructure.Security
{
    /// <summary>
    /// Identity revalidating authentication state provider
    /// </summary>
    public class IdentityRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<IdentityOptions> _optionsAccessor;

        public IdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _optionsAccessor = optionsAccessor;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // Get the user from a new scope to ensure it fetches fresh data
            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await ValidateSecurityStampAsync(userManager, authenticationState.User);
        }

        private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user == null)
            {
                return false;
            }
            else if (!userManager.SupportsUserSecurityStamp)
            {
                return true;
            }
            else
            {
                var principalStamp = principal.FindFirstValue(_optionsAccessor.Value.ClaimsIdentity.SecurityStampClaimType);
                var userStamp = await userManager.GetSecurityStampAsync(user);
                return principalStamp == userStamp;
            }
        }
    }

    /// <summary>
    /// Module access requirement for authorization
    /// </summary>
    public class ModuleAccessRequirement : IAuthorizationRequirement
    {
        public string ModuleName { get; set; }
        public PermissionType? RequiredPermission { get; set; }

        public ModuleAccessRequirement()
        {
        }

        public ModuleAccessRequirement(string moduleName, PermissionType? requiredPermission = null)
        {
            ModuleName = moduleName;
            RequiredPermission = requiredPermission;
        }
    }

    /// <summary>
    /// Module access authorization handler
    /// </summary>
    public class ModuleAccessHandler : AuthorizationHandler<ModuleAccessRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public ModuleAccessHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ModuleAccessRequirement requirement)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var moduleAuthService = scope.ServiceProvider.GetRequiredService<IModuleAuthorizationService>();

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            // Check if module name is provided
            if (!string.IsNullOrEmpty(requirement.ModuleName))
            {
                var hasAccess = await moduleAuthService.CanAccessModuleAsync(userId, requirement.ModuleName);

                if (hasAccess)
                {
                    // If specific permission is required, check it
                    if (requirement.RequiredPermission.HasValue)
                    {
                        hasAccess = await moduleAuthService.HasPermissionAsync(
                            userId,
                            requirement.ModuleName,
                            requirement.RequiredPermission.Value);
                    }
                }

                if (hasAccess)
                {
                    context.Succeed(requirement);
                }
            }
            else
            {
                // No specific module requirement, just check if user is authenticated
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Module authorization service implementation
    /// </summary>
    public class ModuleAuthorizationService : IModuleAuthorizationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<ModuleAuthorizationService> _logger;

        public ModuleAuthorizationService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<ModuleAuthorizationService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<bool> CanAccessModuleAsync(string userId, string moduleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                // Check if user is administrator
                if (await _userManager.IsInRoleAsync(user, "Administrator"))
                    return true;

                // Check module permissions
                var module = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName && m.IsEnabled);

                if (module == null)
                    return false;

                // Check user-specific permissions
                var userPermission = await _dbContext.ModulePermissions
                    .AnyAsync(p => p.ModuleId == module.Id &&
                                   p.UserId == userId &&
                                   p.IsGranted);

                if (userPermission)
                    return true;

                // Check role-based permissions
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var roleName in userRoles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var rolePermission = await _dbContext.ModulePermissions
                            .AnyAsync(p => p.ModuleId == module.Id &&
                                          p.RoleId == role.Id &&
                                          p.IsGranted);

                        if (rolePermission)
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking module access for user {UserId} and module {ModuleName}",
                    userId, moduleName);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string userId, string moduleName, PermissionType permission)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                // Administrators have all permissions
                if (await _userManager.IsInRoleAsync(user, "Administrator"))
                    return true;

                var module = await _dbContext.Modules
                    .FirstOrDefaultAsync(m => m.Name == moduleName && m.IsEnabled);

                if (module == null)
                    return false;

                // Check user-specific permission
                var userPermission = await _dbContext.ModulePermissions
                    .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                              p.UserId == userId &&
                                              p.PermissionType == permission.ToString());

                if (userPermission?.IsGranted == true)
                    return true;

                // Check role-based permissions
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var roleName in userRoles)
                {
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role != null)
                    {
                        var rolePermission = await _dbContext.ModulePermissions
                            .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                                     p.RoleId == role.Id &&
                                                     p.PermissionType == permission.ToString());

                        if (rolePermission?.IsGranted == true)
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId} and module {ModuleName}",
                    permission, userId, moduleName);
                return false;
            }
        }

        public async Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission)
        {
            var module = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName);

            if (module == null)
                throw new InvalidOperationException($"Module {moduleName} not found");

            var existingPermission = await _dbContext.ModulePermissions
                .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                          p.UserId == userId &&
                                          p.PermissionType == permission.ToString());

            if (existingPermission != null)
            {
                existingPermission.IsGranted = true;
                existingPermission.GrantedDate = DateTime.UtcNow;
            }
            else
            {
                _dbContext.ModulePermissions.Add(new ModulePermission
                {
                    ModuleId = module.Id,
                    UserId = userId,
                    PermissionType = permission.ToString(),
                    IsGranted = true,
                    GrantedDate = DateTime.UtcNow,
                    GrantedBy = "System"
                });
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Granted {Permission} permission to user {UserId} for module {ModuleName}",
                permission, userId, moduleName);
        }

        public async Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission)
        {
            var module = await _dbContext.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName);

            if (module == null)
                return;

            var existingPermission = await _dbContext.ModulePermissions
                .FirstOrDefaultAsync(p => p.ModuleId == module.Id &&
                                          p.UserId == userId &&
                                          p.PermissionType == permission.ToString());

            if (existingPermission != null)
            {
                existingPermission.IsGranted = false;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Revoked {Permission} permission from user {UserId} for module {ModuleName}",
                    permission, userId, moduleName);
            }
        }

        public async Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId)
        {
            return await _dbContext.ModulePermissions
                .Include(p => p.Module)
                .Where(p => p.UserId == userId && p.IsGranted)
                .ToListAsync();
        }
    }
}