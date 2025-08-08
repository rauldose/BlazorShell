using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;
using BlazorShell.Modules.Dashboard.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Dashboard.Services.Implementations;

public class WidgetService : IWidgetService
{
    private readonly ILogger<WidgetService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    private const string CacheKeyPrefix = "user_widgets_";

    public WidgetService(ILogger<WidgetService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    private List<string> GetUserWidgetList(string userId)
    {
        var key = CacheKeyPrefix + userId;
        if (!_cache.TryGetValue(key, out List<string>? widgets))
        {
            widgets = new List<string>();
            _cache.Set(key, widgets, _cacheOptions);
        }
        return widgets;
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

        var widgets = GetUserWidgetList(userId);
        if (!widgets.Contains(widgetId))
        {
            widgets.Add(widgetId);
            _logger.LogInformation("Widget {WidgetId} added for user {UserId}", widgetId, userId);
            return true;
        }

        return false;
    }

    public async Task<bool> RemoveWidgetFromDashboardAsync(string userId, string widgetId)
    {
        await Task.Delay(50);

        var widgets = GetUserWidgetList(userId);
        var removed = widgets.Remove(widgetId);
        if (removed)
        {
            _logger.LogInformation("Widget {WidgetId} removed for user {UserId}", widgetId, userId);
        }
        return removed;
    }

    public async Task<IEnumerable<Widget>> GetUserWidgetsAsync(string userId)
    {
        await Task.Delay(50);

        var widgetIds = GetUserWidgetList(userId);
        if (widgetIds.Count == 0)
        {
            return await GetAvailableWidgetsAsync();
        }

        var widgets = new List<Widget>();
        foreach (var widgetId in widgetIds)
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
