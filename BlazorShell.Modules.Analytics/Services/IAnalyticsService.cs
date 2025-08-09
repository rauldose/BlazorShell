using BlazorShell.Modules.Analytics.Domain.Entities;
using BlazorShell.Modules.Analytics.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardMetrics> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate);
        Task<List<SalesData>> GetSalesDataAsync(DateTime startDate, DateTime endDate, string filter = null);
        Task<byte[]> ExportToPdfAsync(DashboardMetrics metrics);
        Task<byte[]> ExportToExcelAsync(List<SalesData> data);
        Task<Dictionary<string, decimal>> GetKpiMetricsAsync();
        Task<List<ChartDataPoint>> GetTrendDataAsync(string metric, int periods);
    }
}
