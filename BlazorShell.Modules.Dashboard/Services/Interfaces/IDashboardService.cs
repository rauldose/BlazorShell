using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;

namespace BlazorShell.Modules.Dashboard.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task<IEnumerable<Widget>> GetWidgetsAsync();
    Task<bool> UpdateWidgetAsync(Widget widget);
    Task<DashboardStats> GetStatsAsync();
}
