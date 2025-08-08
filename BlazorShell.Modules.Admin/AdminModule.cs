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
        public int Order => 999;

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
            // Method 1: Build hierarchy manually
            return BuildHierarchicalNavigation();

            // Method 2: Build flat list then convert to hierarchy
            // return BuildNavigationWithHelperMethod();
        }

        private IEnumerable<NavigationItem> BuildHierarchicalNavigation()
        {
            // Create parent item with children directly
            var adminParent = new NavigationItem
            {            
                Name = "Administration",
                DisplayName = "Administration",
                Url = "#",
                Icon = "bi bi-gear-fill",
                Order = 0,
                Type = NavigationType.SideMenu,
                MinimumRole = "Administrator",
                ParentId = null,
                IsVisible = true,
                Children = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Name = "ModuleManager",
                        DisplayName = "Module Manager",
                        Url = "/admin/modules",
                        Icon = "bi bi-puzzle",
                        Order = 1,
                        Type = NavigationType.SideMenu,
                             MinimumRole = "Administrator",
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
                             MinimumRole = "Administrator",
                        ParentId = 1,
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
                             MinimumRole = "Administrator",
                        ParentId = 1,
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
                             MinimumRole = "Administrator",
                        ParentId = 1,
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
                             MinimumRole = "Administrator",
                        ParentId = 1,
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
                             MinimumRole = "Administrator",
                        ParentId = 1,
                        IsVisible = true
                    }
                }
            };

            // Set parent reference on children (optional but useful for navigation)
            foreach (var child in adminParent.Children)
            {
                child.Parent = adminParent;
            }

            // Return only root level items - children are accessible via Children property
            return new List<NavigationItem> { adminParent };
        }

        private IEnumerable<NavigationItem> BuildNavigationWithHelperMethod()
        {
            // Create flat list first
            var allItems = new List<NavigationItem>
            {
                new NavigationItem
                {
                    Id = 1,
                    Name = "Administration",
                    DisplayName = "Administration",
                    Url = "#",
                    Icon = "bi bi-gear-fill",
                    Order = 0,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = null,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 2,
                    Name = "ModuleManager",
                    DisplayName = "Module Manager",
                    Url = "/admin/modules",
                    Icon = "bi bi-puzzle",
                    Order = 1,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 3,
                    Name = "UserManagement",
                    DisplayName = "User Management",
                    Url = "/admin/users",
                    Icon = "bi bi-people",
                    Order = 2,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 4,
                    Name = "RoleManagement",
                    DisplayName = "Role Management",
                    Url = "/admin/roles",
                    Icon = "bi bi-shield-lock",
                    Order = 3,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 5,
                    Name = "AccessConfiguration",
                    DisplayName = "Access Configuration",
                    Url = "/admin/access",
                    Icon = "bi bi-lock",
                    Order = 4,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 6,
                    Name = "Settings",
                    DisplayName = "Settings",
                    Url = "/admin/settings",
                    Icon = "bi bi-sliders",
                    Order = 5,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                },
                new NavigationItem
                {
                    Id = 7,
                    Name = "AuditLog",
                    DisplayName = "Audit Log",
                    Url = "/admin/audit",
                    Icon = "bi bi-journal-text",
                    Order = 6,
                    Type = NavigationType.SideMenu,
                         MinimumRole = "Administrator",
                    ParentId = 1,
                    IsVisible = true
                }
            };

            // Build hierarchy
            return BuildHierarchy(allItems);
        }

        private IEnumerable<NavigationItem> BuildHierarchy(List<NavigationItem> flatList)
        {
            var lookup = flatList.ToLookup(i => i.ParentId);

            // Process each item to populate its Children collection
            foreach (var item in flatList)
            {
                item.Children = lookup[item.Id].OrderBy(c => c.Order).ToList();

                // Set parent reference
                foreach (var child in item.Children)
                {
                    child.Parent = item;
                }
            }

            // Return only root items (ParentId == null)
            return flatList.Where(i => i.ParentId == null).OrderBy(i => i.Order);
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            var assembly = typeof(AdminModule).Assembly;
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) &&
                           !t.IsAbstract &&
                           t.GetCustomAttributes<RouteAttribute>().Any())
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
            services.AddScoped<IModuleManagementService, ModuleManagementService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IAuditService, AuditService>();
            _logger?.LogInformation("Admin module services registered");
        }
    }
}