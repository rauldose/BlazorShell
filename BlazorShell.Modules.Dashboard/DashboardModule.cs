using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BlazorShell.Modules.Dashboard
{
    /// <summary>
    /// Sample Dashboard Module implementation
    /// </summary>
    [ModuleMetadata(
        Name = "Dashboard",
        DisplayName = "Dashboard Module",
        Description = "Provides dashboard functionality with widgets",
        Version = "1.0.0",
        Author = "BlazorShell Team",
        Icon = "bi bi-speedometer2",
    Category = "Core",
        Order = 1)]
    public class DashboardModule : IModule, IServiceModule, IConfigurableModule
    {
        private readonly ILogger<DashboardModule> _logger;
        private Dictionary<string, object> _configuration;
        private bool _isActive;

        public string Name => "Dashboard";
        public string DisplayName => "Dashboard Module";
        public string Description => "Provides dashboard functionality with widgets";
        public string Version => "1.0.0";
        public string Author => "BlazorShell Team";
        public string Icon => "bi bi-speedometer2";
        public string Category => "Core";
        public int Order => 1;

        //public Type ConfigurationComponentType => typeof(DashboardConfigurationComponent);
        public Type ConfigurationComponentType => typeof(DashboardComponent);

        public DashboardModule()
        {
            // Logger will be injected if using DI, otherwise create a default one
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<DashboardModule>();
        }

        public DashboardModule(ILogger<DashboardModule> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                _logger.LogInformation("Initializing Dashboard module");

                // Load default configuration
                _configuration = GetDefaultSettings();

                // Perform any initialization logic
                // For example, check database connections, load cached data, etc.

                _logger.LogInformation("Dashboard module initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Dashboard module");
                return false;
            }
        }

        public async Task<bool> ActivateAsync()
        {
            try
            {
                _logger.LogInformation("Activating Dashboard module");
                _isActive = true;

                // Perform activation logic
                // For example, start background services, timers, etc.

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate Dashboard module");
                return false;
            }
        }

        public async Task<bool> DeactivateAsync()
        {
            try
            {
                _logger.LogInformation("Deactivating Dashboard module");
                _isActive = false;

                // Perform cleanup
                // For example, stop background services, dispose resources, etc.

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate Dashboard module");
                return false;
            }
        }

        public IEnumerable<NavigationItem> GetNavigationItems()
        {
            return new List<NavigationItem>
            {
                new NavigationItem
                {
                    Name = "dashboard",
                    DisplayName = "Dashboard",
                    Url = "/",
                    Icon = "bi bi-speedometer2",
                    Order = 1,
                    Type = NavigationType.Both,
                    IsVisible = true,
                    RequiredPermission = "Dashboard.Read"
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
                    RequiredPermission = "Dashboard.Read",
                    ParentId = 1
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
                    RequiredPermission = "Dashboard.Admin",
                    ParentId = 1
                }
            };
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            return new List<Type>
            {
                typeof(DashboardComponent),
                //typeof(DashboardAnalyticsComponent),
                //typeof(DashboardWidgetsComponent),
                //typeof(DashboardConfigurationComponent)
            };
        }

        public Dictionary<string, object> GetDefaultSettings()
        {
            return new Dictionary<string, object>
            {
                ["RefreshInterval"] = 30000,
                ["DefaultWidgets"] = new[] { "stats", "charts", "activities" },
                ["MaxWidgets"] = 6,
                ["EnableAutoRefresh"] = true,
                ["ChartType"] = "line"
            };
        }

        public void RegisterServices(IServiceCollection services)
        {
            // Register module-specific services
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IWidgetService, WidgetService>();
            services.AddSingleton<IDashboardDataProvider, DashboardDataProvider>();
        }

        public async Task<bool> ValidateConfigurationAsync(Dictionary<string, object> configuration)
        {
            // Validate configuration
            if (configuration == null) return false;

            if (configuration.TryGetValue("RefreshInterval", out var interval))
            {
                if (interval is int intValue && intValue < 5000)
                {
                    _logger.LogWarning("Refresh interval is too low, minimum is 5000ms");
                    return false;
                }
            }

            if (configuration.TryGetValue("MaxWidgets", out var maxWidgets))
            {
                if (maxWidgets is int maxValue && (maxValue < 1 || maxValue > 20))
                {
                    _logger.LogWarning("MaxWidgets must be between 1 and 20");
                    return false;
                }
            }

            return true;
        }

        public async Task ApplyConfigurationAsync(Dictionary<string, object> configuration)
        {
            if (await ValidateConfigurationAsync(configuration))
            {
                _configuration = configuration;
                _logger.LogInformation("Configuration applied to Dashboard module");
            }
        }
    }

    // Module Services
    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<IEnumerable<Widget>> GetWidgetsAsync();
        Task UpdateWidgetAsync(Widget widget);
    }

    public interface IWidgetService
    {
        Task<Widget> GetWidgetAsync(string widgetId);
        Task<IEnumerable<Widget>> GetAvailableWidgetsAsync();
        Task<bool> AddWidgetToDashboardAsync(string widgetId);
        Task<bool> RemoveWidgetFromDashboardAsync(string widgetId);
    }

    public interface IDashboardDataProvider
    {
        Task<object> GetDataAsync(string dataType);
    }

    // Service Implementations
    public class DashboardService : IDashboardService
    {
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ILogger<DashboardService> logger)
        {
            _logger = logger;
        }

        public async Task<DashboardData> GetDashboardDataAsync()
        {
            // Simulate data fetching
            await Task.Delay(100);

            return new DashboardData
            {
                TotalUsers = 1234,
                ActiveSessions = 56,
                TotalRevenue = 98765.43m,
                GrowthRate = 12.5,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Widget>> GetWidgetsAsync()
        {
            await Task.Delay(50);

            return new List<Widget>
            {
                new Widget { Id = "stats", Name = "Statistics", Type = "stats", Order = 1 },
                new Widget { Id = "charts", Name = "Charts", Type = "chart", Order = 2 },
                new Widget { Id = "activities", Name = "Recent Activities", Type = "list", Order = 3 }
            };
        }

        public async Task UpdateWidgetAsync(Widget widget)
        {
            _logger.LogInformation("Updating widget {WidgetId}", widget.Id);
            await Task.Delay(50);
        }
    }

    public class WidgetService : IWidgetService
    {
        public async Task<Widget> GetWidgetAsync(string widgetId)
        {
            await Task.Delay(50);
            return new Widget { Id = widgetId, Name = $"Widget {widgetId}", Type = "custom" };
        }

        public async Task<IEnumerable<Widget>> GetAvailableWidgetsAsync()
        {
            await Task.Delay(50);
            return new List<Widget>
            {
                new Widget { Id = "calendar", Name = "Calendar", Type = "calendar" },
                new Widget { Id = "tasks", Name = "Tasks", Type = "list" },
                new Widget { Id = "notifications", Name = "Notifications", Type = "list" }
            };
        }

        public async Task<bool> AddWidgetToDashboardAsync(string widgetId)
        {
            await Task.Delay(50);
            return true;
        }

        public async Task<bool> RemoveWidgetFromDashboardAsync(string widgetId)
        {
            await Task.Delay(50);
            return true;
        }
    }

    public class DashboardDataProvider : IDashboardDataProvider
    {
        public async Task<object> GetDataAsync(string dataType)
        {
            await Task.Delay(100);

            return dataType switch
            {
                "stats" => new { Users = 100, Sessions = 50, Revenue = 10000 },
                "chart" => new[] { 10, 20, 30, 40, 50 },
                _ => null
            };
        }
    }

    // Data Models
    public class DashboardData
    {
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class Widget
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }
}