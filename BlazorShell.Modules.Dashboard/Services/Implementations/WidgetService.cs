using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BlazorShell.Modules.Dashboard.Models;
using BlazorShell.Modules.Dashboard.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Dashboard.Services.Implementations;

public class WidgetService : IWidgetService
{
    private readonly ILogger<WidgetService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _userWidgets = new();

    public WidgetService(ILogger<WidgetService> logger)
    {
        _logger = logger;
    }

    public async Task<Widget?> GetWidgetAsync(string widgetId)
    {
        await Task.Delay(50).ConfigureAwait(false);

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
        await Task.Delay(50).ConfigureAwait(false);

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
        await Task.Delay(50).ConfigureAwait(false);

        var widgets = _userWidgets.GetOrAdd(userId, _ => new HashSet<string>());
        lock (widgets)
        {
            if (widgets.Add(widgetId))
            {
                _logger.LogInformation("Widget {WidgetId} added for user {UserId}", widgetId, userId);
                return true;
            }
        }
        return false;
    }

    public async Task<bool> RemoveWidgetFromDashboardAsync(string userId, string widgetId)
    {
        await Task.Delay(50).ConfigureAwait(false);

        if (_userWidgets.TryGetValue(userId, out var widgets))
        {
            lock (widgets)
            {
                var removed = widgets.Remove(widgetId);
                if (removed)
                {
                    _logger.LogInformation("Widget {WidgetId} removed for user {UserId}", widgetId, userId);
                }
                return removed;
            }
        }
        return false;
    }

    public async Task<IEnumerable<Widget>> GetUserWidgetsAsync(string userId)
    {
        await Task.Delay(50).ConfigureAwait(false);

        if (!_userWidgets.TryGetValue(userId, out var widgetIds) || widgetIds.Count == 0)
        {
            return await GetAvailableWidgetsAsync().ConfigureAwait(false);
        }

        var widgets = new List<Widget>();
        foreach (var widgetId in widgetIds)
        {
            var widget = await GetWidgetAsync(widgetId).ConfigureAwait(false);
            if (widget != null)
            {
                widgets.Add(widget);
            }
        }
        return widgets;
    }
}
