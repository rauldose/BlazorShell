// BlazorShell.Modules.Admin/AdminModule.cs
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using BlazorShell.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using BlazorShell.Modules.Admin.Services;

namespace BlazorShell.Modules.Admin
{
    public class AdminModule : IModule, IServiceModule
    {
        private ILogger<AdminModule>? _logger;
        private IServiceProvider? _serviceProvider;

        public string Name => "Admin";
        public string DisplayName => "Administration";
        public string Description => "System administration module for managing modules, users, and settings";
        public string Version => "1.0.0";
        public string Author => "BlazorShell Team";
        public string Icon => "bi bi-gear-fill";
        public string Category => "System";
        public int Order => 999; // Load last to ensure all other modules are available

        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<AdminModule>>();

            _logger?.LogInformation("Initializing Admin module v{Version}", Version);

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ActivateAsync()
        {
            _logger?.LogInformation("Admin module activated");
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> DeactivateAsync()
        {
            _logger?.LogInformation("Admin module deactivated");
            await Task.CompletedTask;
            return true;
        }

        public IEnumerable<NavigationItem> GetNavigationItems()
        {
            return new List<NavigationItem>
            {
                new NavigationItem
                {
                    Name = "admin",
                    DisplayName = "Administration",
                    Url = "/admin",
                    Icon = "bi bi-gear-fill",
                    Order = 900,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    IsVisible = true
                },
                new NavigationItem
                {
                    Name = "admin-modules",
                    DisplayName = "Modules",
                    Url = "/admin/modules",
                    Icon = "bi bi-puzzle",
                    Order = 901,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Name = "admin-users",
                    DisplayName = "Users",
                    Url = "/admin/users",
                    Icon = "bi bi-people",
                    Order = 902,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Name = "admin-roles",
                    DisplayName = "Roles",
                    Url = "/admin/roles",
                    Icon = "bi bi-shield-lock",
                    Order = 903,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Name = "admin-settings",
                    DisplayName = "Settings",
                    Url = "/admin/settings",
                    Icon = "bi bi-sliders",
                    Order = 904,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Name = "admin-audit",
                    DisplayName = "Audit Logs",
                    Url = "/admin/audit",
                    Icon = "bi bi-journal-text",
                    Order = 905,
                    Type = NavigationType.SideMenu,
                    RequiredRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
        {
            Name = "admin-performance",
            DisplayName = "Module Performance",
            Url = "/admin/modules/performance",
            Icon = "bi bi-speedometer2",
            Order = 906,
            Type = NavigationType.SideMenu,
            RequiredRole = "Administrator",
            ParentId = null,
            IsVisible = true
        }
            };
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            var assembly = typeof(AdminModule).Assembly;
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Microsoft.AspNetCore.Components.ComponentBase)) &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<Microsoft.AspNetCore.Components.RouteAttribute>() != null)
                .ToList();
        }

        public Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                ["EnableModuleUpload"] = true,
                ["MaxUploadSizeMB"] = 10,
                ["EnableAutoBackup"] = true,
                ["BackupRetentionDays"] = 30,
                ["EnableAuditLogging"] = true,
                ["PageSize"] = 20
            };
        }

        public void RegisterServices(IServiceCollection services)
        {
            // Register admin-specific services
            services.AddScoped<IModuleManagementService, ModuleManagementService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IAuditService, AuditService>();
            
            // Register UI services for better separation of concerns
            services.AddScoped<IModuleManagementUIService, ModuleManagementUIService>();
            services.AddScoped<IModuleUploadService, ModuleUploadService>();

            _logger?.LogInformation("Admin module services registered");
        }
    }
}