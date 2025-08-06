using BlazorShell.Core.Entities;
using BlazorShell.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Permissions
{
    [ModuleMetadata(
        Name = "Permissions",
        DisplayName = "Permissions Module",
        Description = "Manages permissions for other modules",
        Version = "1.0.0",
        Author = "BlazorShell Team",
        Icon = "bi bi-shield-lock",
        Category = "Administration",
        Order = 3)]
    public class PermissionsModule : IModule, IServiceModule
    {
        private readonly ILogger<PermissionsModule> _logger;
        private bool _isActive;

        public string Name => "Permissions";
        public string DisplayName => "Permissions Module";
        public string Description => "Manages permissions for other modules";
        public string Version => "1.0.0";
        public string Author => "BlazorShell Team";
        public string Icon => "bi bi-shield-lock";
        public string Category => "Administration";
        public int Order => 3;

        public PermissionsModule()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<PermissionsModule>();
        }

        public PermissionsModule(ILogger<PermissionsModule> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("Initializing Permissions module");
            return await Task.FromResult(true);
        }

        public async Task<bool> ActivateAsync()
        {
            _logger.LogInformation("Activating Permissions module");
            _isActive = true;
            return await Task.FromResult(true);
        }

        public async Task<bool> DeactivateAsync()
        {
            _logger.LogInformation("Deactivating Permissions module");
            _isActive = false;
            return await Task.FromResult(true);
        }

        public IEnumerable<NavigationItem> GetNavigationItems()
        {
            return new List<NavigationItem>
            {
                new NavigationItem
                {
                    Name = "permissions",
                    DisplayName = "Permissions",
                    Url = "/admin/permissions",
                    Icon = "bi bi-shield-lock",
                    Order = 50,
                    Type = NavigationType.Both,
                    IsVisible = true,
                    RequiredPermission = "Permissions.Manage"
                }
            };
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            return new List<Type>
            {
                typeof(PermissionsComponent)
            };
        }

        public Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>();
        }

        public void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionManagementService, PermissionManagementService>();
        }
    }

    public interface IPermissionManagementService
    {
        Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId);
        Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission);
        Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission);
    }

    public class PermissionManagementService : IPermissionManagementService
    {
        private readonly IModuleAuthorizationService _authService;

        public PermissionManagementService(IModuleAuthorizationService authService)
        {
            _authService = authService;
        }

        public Task<IEnumerable<ModulePermission>> GetUserPermissionsAsync(string userId)
        {
            return _authService.GetUserPermissionsAsync(userId);
        }

        public Task GrantPermissionAsync(string userId, string moduleName, PermissionType permission)
        {
            return _authService.GrantPermissionAsync(userId, moduleName, permission);
        }

        public Task RevokePermissionAsync(string userId, string moduleName, PermissionType permission)
        {
            return _authService.RevokePermissionAsync(userId, moduleName, permission);
        }
    }
}
