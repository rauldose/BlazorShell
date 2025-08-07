using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using System.Collections.Generic;
using BlazorShell.Core.Components;
using BlazorShell.Modules.Dashboard.Services.Interfaces;
using BlazorShell.Modules.Dashboard.Services.Implementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace BlazorShell.Modules.Dashboard
{
    /// <summary>
    /// Dashboard Module implementation as external module
    /// </summary>
    public class DashboardModule : IModule, IServiceModule
    {
        private ILogger<DashboardModule>? _logger;
        private Dictionary<string, object> _configuration;
        private bool _isActive;
        private IServiceProvider? _serviceProvider;

        public string Name => "Dashboard";
        public string DisplayName => "Dashboard Module";
        public string Description => "Provides dashboard functionality with widgets and analytics";
        public string Version => "1.0.0";
        public string Author => "BlazorShell Team";
        public string Icon => "bi bi-speedometer2";
        public string Category => "Core";
        public int Order => 1;

        public DashboardModule()
        {
            _configuration = GetDefaultSettings();
        }

        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                _serviceProvider = serviceProvider;
                _logger = serviceProvider.GetService<ILogger<DashboardModule>>();

                _logger?.LogInformation("Initializing Dashboard module v{Version}", Version);

                // Perform any initialization logic
                await Task.Delay(100); // Simulate initialization work

                _logger?.LogInformation("Dashboard module initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Dashboard module");
                return false;
            }
        }

        public async Task<bool> ActivateAsync()
        {
            try
            {
                _logger?.LogInformation("Activating Dashboard module");
                _isActive = true;

                // Start any background services or timers
                await Task.CompletedTask;

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to activate Dashboard module");
                return false;
            }
        }

        public async Task<bool> DeactivateAsync()
        {
            try
            {
                _logger?.LogInformation("Deactivating Dashboard module");
                _isActive = false;

                // Stop any background services or timers
                await Task.CompletedTask;

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deactivate Dashboard module");
                return false;
            }
        }

        public IEnumerable<NavigationItem> GetNavigationItems()
        {
            var items = new List<NavigationItem>
            {
                new NavigationItem
                {
                    Name = "dashboard",
                    DisplayName = "Dashboard",
                    Url = "/dashboard",
                    Icon = "bi bi-speedometer2",
                    Order = 1,
                    Type = NavigationType.Both,
                    IsVisible = true,
                    RequiredPermission = null // Public access for demo
                },
                new NavigationItem
                {
                    Name = "dashboard-analytics",
                    DisplayName = "Analytics",
                    Url = "/dashboard/analytics",
                    Icon = "bi bi-graph-up",
                    Order = 2,
                    Type = NavigationType.SideMenu,
                    IsVisible = true,
                    RequiredPermission = null,
                    ParentId = null
                },
                new NavigationItem
                {
                    Name = "dashboard-widgets",
                    DisplayName = "Widgets",
                    Url = "/dashboard/widgets",
                    Icon = "bi bi-grid-3x3",
                    Order = 3,
                    Type = NavigationType.SideMenu,
                    IsVisible = true,
                    RequiredPermission = null,
                    ParentId = null
                },
                new NavigationItem
                {
                    Name = "dashboard-reports",
                    DisplayName = "Reports",
                    Url = "/dashboard/reports",
                    Icon = "bi bi-file-earmark-bar-graph",
                    Order = 4,
                    Type = NavigationType.Both,
                    IsVisible = true,
                    RequiredPermission = null
                }
            };

            return items;
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            // Return the component types from this assembly
            // These would be the compiled Razor component types
            var assembly = typeof(DashboardModule).Assembly;

            // Get all types that are Razor components (they inherit from ComponentBase)
            var componentTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Microsoft.AspNetCore.Components.ComponentBase)) &&
                           !t.IsAbstract &&
                           t.GetCustomAttribute<Microsoft.AspNetCore.Components.RouteAttribute>() != null)
                .ToList();

            // If no components found in assembly, return empty list
            // In production, the Razor components would be compiled into the assembly
            if (!componentTypes.Any())
            {
                _logger?.LogWarning("No routable components found in Dashboard module assembly");
            }

            return componentTypes;
        }

        public Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                ["RefreshInterval"] = 30000,
                ["DefaultWidgets"] = new[] { "stats", "charts", "activities", "notifications" },
                ["MaxWidgets"] = 6,
                ["EnableAutoRefresh"] = true,
                ["ChartType"] = "line",
                ["Theme"] = "default"
            };
        }

        public void RegisterServices(IServiceCollection services)
        {
            // Register module-specific services
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IWidgetService, WidgetService>();
            services.AddSingleton<IDashboardDataProvider, DashboardDataProvider>();

            _logger?.LogInformation("Dashboard module services registered");
        }
    }
}
