// BlazorShell.Modules.Admin/AdminModule.cs
using BlazorShell.Application.Interfaces;
using BlazorShell.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using BlazorShell.Modules.Admin.Services;
using Microsoft.AspNetCore.Components;
using BlazorShell.Modules.Admin.Services.Implementations;
using BlazorShell.Modules.Admin.Services.Interfaces;

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
                            Name = "ModuleManager",
                            DisplayName = "Module Manager",
                            Url = "/admin/modules",
                            Icon = "bi bi-puzzle",
                            Order = 1,
                            Type = NavigationType.SideMenu,
                            RequiredRole = "Administrator",
                            ParentId = 1,
                            IsVisible = true
                        },
                        new NavigationItem
                        {
                            Name = "UserManagement",
                            DisplayName = "User Management",
                            Url = "/admin/users",
                            Icon = "bi bi-people",
                            Order = 2,
                            Type = NavigationType.SideMenu,
                            RequiredRole = "Administrator",
                            ParentId = null,
                            IsVisible = true
                        },
                        new NavigationItem
                        {
                            Name = "RoleManagement",
                            DisplayName = "Role Management",
                            Url = "/admin/roles",
                            Icon = "bi bi-shield-lock",
                            Order = 3,
                            Type = NavigationType.SideMenu,
                            RequiredRole = "Administrator",
                            ParentId = null,
                            IsVisible = true
                        },
                        new NavigationItem
                        {
                            Name = "AccessConfiguration",
                            DisplayName = "Access Configuration",
                            Url = "/admin/access",
                            Icon = "bi bi-lock",
                            Order = 4,
                            Type = NavigationType.SideMenu,
                            RequiredRole = "Administrator",
                            ParentId = null,
                            IsVisible = true
                        },
                        new NavigationItem
                        {
                            Name = "Settings",
                            DisplayName = "Settings",
                            Url = "/admin/settings",
                            Icon = "bi bi-sliders",
                            Order = 5,
                            Type = NavigationType.SideMenu,
                            RequiredRole = "Administrator",
                            ParentId = null,
                            IsVisible = true
                        },
                        new NavigationItem
                        {
                            Name = "AuditLog",
                            DisplayName = "Audit Log",
                            Url = "/admin/audit",
                            Icon = "bi bi-journal-text",
                            Order = 6,
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

            // Fixed: Use GetCustomAttributes (plural) to handle multiple route attributes
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) &&
                           !t.IsAbstract &&
                           t.GetCustomAttributes<RouteAttribute>().Any()) // Changed to GetCustomAttributes
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
            //services.AddScoped<IAccessConfigurationService, AccessConfigurationService>();

            _logger?.LogInformation("Admin module services registered");
        }
    }
}