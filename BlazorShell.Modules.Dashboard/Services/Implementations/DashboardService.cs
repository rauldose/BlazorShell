using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;
using BlazorShell.Modules.Dashboard.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Dashboard.Services.Implementations;

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
        await Task.Delay(100);

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
