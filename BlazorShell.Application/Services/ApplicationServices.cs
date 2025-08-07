using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Application.Services
{
    /// <summary>
    /// Application service for module management use cases
    /// </summary>
    public class ModuleManagementService
    {
        private readonly IModuleLoader _moduleLoader;
        private readonly IModuleRegistry _moduleRegistry;
        private readonly ILogger<ModuleManagementService> _logger;

        public ModuleManagementService(
            IModuleLoader moduleLoader,
            IModuleRegistry moduleRegistry,
            ILogger<ModuleManagementService> logger)
        {
            _moduleLoader = moduleLoader;
            _moduleRegistry = moduleRegistry;
            _logger = logger;
        }

        public async Task<IEnumerable<IModule>> GetLoadedModulesAsync()
        {
            return await _moduleLoader.GetLoadedModulesAsync();
        }

        public async Task<bool> LoadModuleAsync(string assemblyPath)
        {
            try
            {
                var module = await _moduleLoader.LoadModuleAsync(assemblyPath);
                return module != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load module from {AssemblyPath}", assemblyPath);
                return false;
            }
        }

        public async Task<bool> UnloadModuleAsync(string moduleName)
        {
            try
            {
                return await _moduleLoader.UnloadModuleAsync(moduleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload module {ModuleName}", moduleName);
                return false;
            }
        }

        public async Task ReloadModuleAsync(string moduleName)
        {
            try
            {
                await _moduleLoader.ReloadModuleAsync(moduleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload module {ModuleName}", moduleName);
                throw;
            }
        }
    }

    /// <summary>
    /// Application service for navigation management
    /// </summary>
    public class NavigationManagementService
    {
        private readonly INavigationService _navigationService;
        private readonly IModuleAuthorizationService _authorizationService;
        private readonly ILogger<NavigationManagementService> _logger;

        public NavigationManagementService(
            INavigationService navigationService,
            IModuleAuthorizationService authorizationService,
            ILogger<NavigationManagementService> logger)
        {
            _navigationService = navigationService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<IEnumerable<NavigationItem>> GetUserNavigationAsync(string userId, NavigationType type)
        {
            try
            {
                return await _navigationService.GetUserNavigationItemsAsync(userId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation for user {UserId}", userId);
                return Enumerable.Empty<NavigationItem>();
            }
        }

        public async Task<bool> CanAccessNavigationAsync(string userId, NavigationItem item)
        {
            try
            {
                return await _navigationService.CanAccessNavigationItemAsync(item, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check navigation access for user {UserId}", userId);
                return false;
            }
        }
    }

    /// <summary>
    /// Application service for user management use cases
    /// </summary>
    public class UserManagementApplicationService
    {
        private readonly IModuleAuthorizationService _authorizationService;
        private readonly ILogger<UserManagementApplicationService> _logger;

        public UserManagementApplicationService(
            IModuleAuthorizationService authorizationService,
            ILogger<UserManagementApplicationService> logger)
        {
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<bool> GrantModuleAccessAsync(string userId, string moduleName, PermissionType permission)
        {
            try
            {
                await _authorizationService.GrantPermissionAsync(userId, moduleName, permission);
                _logger.LogInformation("Granted {Permission} permission for module {Module} to user {User}", 
                    permission, moduleName, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to grant permission for user {UserId} to module {ModuleName}", 
                    userId, moduleName);
                return false;
            }
        }

        public async Task<bool> RevokeModuleAccessAsync(string userId, string moduleName, PermissionType permission)
        {
            try
            {
                await _authorizationService.RevokePermissionAsync(userId, moduleName, permission);
                _logger.LogInformation("Revoked {Permission} permission for module {Module} from user {User}", 
                    permission, moduleName, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke permission for user {UserId} from module {ModuleName}", 
                    userId, moduleName);
                return false;
            }
        }

        public async Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId)
        {
            try
            {
                return await _authorizationService.GetUserPermissionsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
                return Enumerable.Empty<ModulePermission>();
            }
        }
    }
}