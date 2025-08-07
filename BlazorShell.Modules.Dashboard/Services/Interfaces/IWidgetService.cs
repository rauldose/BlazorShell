using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;

namespace BlazorShell.Modules.Dashboard.Services.Interfaces;

public interface IWidgetService
{
    Task<Widget?> GetWidgetAsync(string widgetId);
    Task<IEnumerable<Widget>> GetAvailableWidgetsAsync();
    Task<bool> AddWidgetToDashboardAsync(string userId, string widgetId);
    Task<bool> RemoveWidgetFromDashboardAsync(string userId, string widgetId);
    Task<IEnumerable<Widget>> GetUserWidgetsAsync(string userId);
}
