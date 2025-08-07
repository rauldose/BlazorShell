using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;

namespace BlazorShell.Modules.Dashboard.Services.Interfaces;

public interface IDashboardDataProvider
{
    Task<object?> GetDataAsync(string dataType);
    Task<ChartData> GetChartDataAsync(string chartType);
}
