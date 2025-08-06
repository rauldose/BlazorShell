
using BlazorShell.Core.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using BlazorShell.Domain.Entities;
using BlazorShell.Application.Interfaces;

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

    #region Services

    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<IEnumerable<Widget>> GetWidgetsAsync();
        Task<bool> UpdateWidgetAsync(Widget widget);
        Task<DashboardStats> GetStatsAsync();
    }

    public interface IWidgetService
    {
        Task<Widget?> GetWidgetAsync(string widgetId);
        Task<IEnumerable<Widget>> GetAvailableWidgetsAsync();
        Task<bool> AddWidgetToDashboardAsync(string userId, string widgetId);
        Task<bool> RemoveWidgetFromDashboardAsync(string userId, string widgetId);
        Task<IEnumerable<Widget>> GetUserWidgetsAsync(string userId);
    }

    public interface IDashboardDataProvider
    {
        Task<object?> GetDataAsync(string dataType);
        Task<ChartData> GetChartDataAsync(string chartType);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ILogger<DashboardService> _logger;
        private readonly IDashboardDataProvider _dataProvider;

        public DashboardService(ILogger<DashboardService> logger, IDashboardDataProvider dataProvider)
        {
            _logger = logger;
            _dataProvider = dataProvider;
        }

        public async Task<DashboardData> GetDashboardDataAsync()
        {
            await Task.Delay(100); // Simulate data fetching

            return new DashboardData
            {
                TotalUsers = Random.Shared.Next(1000, 5000),
                ActiveSessions = Random.Shared.Next(50, 200),
                TotalRevenue = (decimal)(Random.Shared.NextDouble() * 100000 + 50000),
                GrowthRate = Random.Shared.NextDouble() * 20 - 5,
                LastUpdated = DateTime.UtcNow,
                RecentActivities = new List<string>
                {
                    "User john.doe logged in",
                    "New order #1234 received",
                    "System backup completed",
                    "Module Dashboard updated"
                }
            };
        }

        public async Task<IEnumerable<Widget>> GetWidgetsAsync()
        {
            await Task.Delay(50);

            return new List<Widget>
            {
                new Widget
                {
                    Id = "stats",
                    Name = "Statistics",
                    Type = "stats",
                    Order = 1,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["refreshRate"] = 5000 }
                },
                new Widget
                {
                    Id = "charts",
                    Name = "Performance Charts",
                    Type = "chart",
                    Order = 2,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["chartType"] = "line" }
                },
                new Widget
                {
                    Id = "activities",
                    Name = "Recent Activities",
                    Type = "list",
                    Order = 3,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["maxItems"] = 10 }
                },
                new Widget
                {
                    Id = "notifications",
                    Name = "Notifications",
                    Type = "notifications",
                    Order = 4,
                    IsVisible = true,
                    Settings = new Dictionary<string, object> { ["showUnreadOnly"] = false }
                }
            };
        }

        public async Task<bool> UpdateWidgetAsync(Widget widget)
        {
            _logger.LogInformation("Updating widget {WidgetId}", widget.Id);
            await Task.Delay(50);
            return true;
        }

        public async Task<DashboardStats> GetStatsAsync()
        {
            await Task.Delay(100);

            return new DashboardStats
            {
                TotalVisits = Random.Shared.Next(10000, 50000),
                UniqueVisitors = Random.Shared.Next(5000, 25000),
                PageViews = Random.Shared.Next(20000, 100000),
                BounceRate = Random.Shared.NextDouble() * 50 + 20,
                AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(2, 15))
            };
        }
    }

    public class WidgetService : IWidgetService
    {
        private readonly ILogger<WidgetService> _logger;
        private static readonly Dictionary<string, List<string>> _userWidgets = new();

        public WidgetService(ILogger<WidgetService> logger)
        {
            _logger = logger;
        }

        public async Task<Widget?> GetWidgetAsync(string widgetId)
        {
            await Task.Delay(50);

            var widget = new Widget
            {
                Id = widgetId,
                Name = $"Widget {widgetId}",
                Type = "custom",
                Order = 1,
                IsVisible = true
            };

            return widget;
        }

        public async Task<IEnumerable<Widget>> GetAvailableWidgetsAsync()
        {
            await Task.Delay(50);

            return new List<Widget>
            {
                new Widget { Id = "calendar", Name = "Calendar", Type = "calendar", Icon = "bi bi-calendar" },
                new Widget { Id = "tasks", Name = "Task List", Type = "list", Icon = "bi bi-list-task" },
                new Widget { Id = "weather", Name = "Weather", Type = "weather", Icon = "bi bi-cloud-sun" },
                new Widget { Id = "news", Name = "News Feed", Type = "feed", Icon = "bi bi-newspaper" },
                new Widget { Id = "clock", Name = "World Clock", Type = "clock", Icon = "bi bi-clock" }
            };
        }

        public async Task<bool> AddWidgetToDashboardAsync(string userId, string widgetId)
        {
            await Task.Delay(50);

            if (!_userWidgets.ContainsKey(userId))
            {
                _userWidgets[userId] = new List<string>();
            }

            if (!_userWidgets[userId].Contains(widgetId))
            {
                _userWidgets[userId].Add(widgetId);
                _logger.LogInformation("Widget {WidgetId} added for user {UserId}", widgetId, userId);
                return true;
            }

            return false;
        }

        public async Task<bool> RemoveWidgetFromDashboardAsync(string userId, string widgetId)
        {
            await Task.Delay(50);

            if (_userWidgets.ContainsKey(userId))
            {
                var removed = _userWidgets[userId].Remove(widgetId);
                if (removed)
                {
                    _logger.LogInformation("Widget {WidgetId} removed for user {UserId}", widgetId, userId);
                }
                return removed;
            }

            return false;
        }

        public async Task<IEnumerable<Widget>> GetUserWidgetsAsync(string userId)
        {
            await Task.Delay(50);

            if (!_userWidgets.ContainsKey(userId) || !_userWidgets[userId].Any())
            {
                // Return default widgets
                return await GetAvailableWidgetsAsync();
            }

            var widgets = new List<Widget>();
            foreach (var widgetId in _userWidgets[userId])
            {
                var widget = await GetWidgetAsync(widgetId);
                if (widget != null)
                {
                    widgets.Add(widget);
                }
            }

            return widgets;
        }
    }

    public class DashboardDataProvider : IDashboardDataProvider
    {
        public async Task<object?> GetDataAsync(string dataType)
        {
            await Task.Delay(100);

            return dataType switch
            {
                "stats" => new { Users = Random.Shared.Next(100, 1000), Sessions = Random.Shared.Next(50, 500), Revenue = Random.Shared.Next(10000, 100000) },
                "chart" => Enumerable.Range(1, 7).Select(i => Random.Shared.Next(10, 100)).ToArray(),
                "activities" => new[] { "Activity 1", "Activity 2", "Activity 3" },
                _ => null
            };
        }

        public async Task<ChartData> GetChartDataAsync(string chartType)
        {
            await Task.Delay(100);

            var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var data = Enumerable.Range(0, 7).Select(_ => Random.Shared.Next(20, 100)).ToArray();

            return new ChartData
            {
                Labels = labels,
                Datasets = new[]
                {
                    new ChartDataset
                    {
                        Label = "This Week",
                        Data = data,
                        BackgroundColor = "rgba(54, 162, 235, 0.2)",
                        BorderColor = "rgba(54, 162, 235, 1)"
                    }
                }
            };
        }
    }

    #endregion

    #region Models

    public class DashboardData
    {
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> RecentActivities { get; set; } = new();
    }

    public class DashboardStats
    {
        public int TotalVisits { get; set; }
        public int UniqueVisitors { get; set; }
        public int PageViews { get; set; }
        public double BounceRate { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
    }

    public class Widget
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class ChartData
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public ChartDataset[] Datasets { get; set; } = Array.Empty<ChartDataset>();
    }

    public class ChartDataset
    {
        public string Label { get; set; } = string.Empty;
        public int[] Data { get; set; } = Array.Empty<int>();
        public string? BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
    }

    #endregion
}