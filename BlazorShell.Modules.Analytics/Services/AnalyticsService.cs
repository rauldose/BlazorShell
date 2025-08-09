using BlazorShell.Modules.Analytics.Domain.Entities;
using BlazorShell.Modules.Analytics.Domain.Interfaces;
using BlazorShell.Modules.Analytics.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repository;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IAnalyticsRepository repository, ILogger<AnalyticsService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var salesData = await _repository.GetSalesDataRangeAsync(startDate, endDate);
            var previousPeriodData = await _repository.GetSalesDataRangeAsync(
                startDate.AddDays(-(endDate - startDate).Days),
                startDate.AddDays(-1)
            );

            var metrics = new DashboardMetrics
            {
                TotalRevenue = salesData.Sum(s => s.Revenue),
                TotalProfit = salesData.Sum(s => s.Profit),
                TotalOrders = salesData.Count(),
                AverageOrderValue = salesData.Any() ? salesData.Average(s => s.Revenue) : 0
            };

            // Calculate growth
            var previousRevenue = previousPeriodData.Sum(s => s.Revenue);
            metrics.GrowthPercentage = previousRevenue > 0
                ? ((metrics.TotalRevenue - previousRevenue) / previousRevenue) * 100
                : 0;

            // Revenue by month
            metrics.RevenueByMonth = salesData
                .GroupBy(s => new { s.Date.Year, s.Date.Month })
                .Select(g => new ChartDataPoint
                {
                    Label = $"{g.Key.Month}/{g.Key.Year}",
                    Value = g.Sum(s => s.Revenue)
                })
                .OrderBy(c => c.Label)
                .ToList();

            // Revenue by category
            metrics.RevenueByCategory = salesData
                .GroupBy(s => s.ProductCategory)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Sum(s => s.Revenue),
                    Color = GetCategoryColor(g.Key)
                })
                .OrderByDescending(c => c.Value)
                .Take(5)
                .ToList();

            // Revenue by region
            metrics.RevenueByRegion = salesData
                .GroupBy(s => s.Region)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Sum(s => s.Revenue)
                })
                .ToList();

            // Top products
            metrics.TopProducts = salesData
                .GroupBy(s => s.ProductCategory)
                .Select(g => new TopPerformer
                {
                    Name = g.Key,
                    Value = g.Sum(s => s.Revenue),
                    Rank = 0
                })
                .OrderByDescending(t => t.Value)
                .Take(10)
                .Select((t, index) => { t.Rank = index + 1; return t; })
                .ToList();

            // Top sales reps
            metrics.TopSalesReps = salesData
                .GroupBy(s => s.SalesRepresentative)
                .Select(g => new TopPerformer
                {
                    Name = g.Key,
                    Value = g.Sum(s => s.Revenue),
                    Rank = 0
                })
                .OrderByDescending(t => t.Value)
                .Take(10)
                .Select((t, index) => { t.Rank = index + 1; return t; })
                .ToList();

            return metrics;
        }

        public async Task<Dictionary<string, decimal>> GetKpiMetricsAsync()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var thisMonthData = await _repository.GetSalesDataRangeAsync(thisMonth, today);
            var lastMonthData = await _repository.GetSalesDataRangeAsync(lastMonth, thisMonth.AddDays(-1));

            return new Dictionary<string, decimal>
            {
                ["MonthlyRevenue"] = thisMonthData.Sum(s => s.Revenue),
                ["MonthlyProfit"] = thisMonthData.Sum(s => s.Profit),
                ["MonthlyOrders"] = thisMonthData.Count(),
                ["ConversionRate"] = CalculateConversionRate(thisMonthData),
                ["CustomerRetention"] = CalculateRetentionRate(thisMonthData, lastMonthData),
                ["ProfitMargin"] = CalculateProfitMargin(thisMonthData)
            };
        }

        private string GetCategoryColor(string category)
        {
            var colors = new Dictionary<string, string>
            {
                ["Electronics"] = "#3B82F6",
                ["Clothing"] = "#10B981",
                ["Food"] = "#F59E0B",
                ["Books"] = "#8B5CF6",
                ["Home"] = "#EF4444"
            };
            return colors.ContainsKey(category) ? colors[category] : "#6B7280";
        }

        private decimal CalculateConversionRate(List<SalesData> data)
        {
            // Simplified calculation - would need visitor data in real scenario
            return data.Any() ? 3.5m : 0;
        }

        private decimal CalculateRetentionRate(List<SalesData> current, List<SalesData> previous)
        {
            // Simplified - would need customer data in real scenario
            return 85.0m;
        }

        private decimal CalculateProfitMargin(List<SalesData> data)
        {
            var totalRevenue = data.Sum(s => s.Revenue);
            var totalProfit = data.Sum(s => s.Profit);
            return totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;
        }

        public Task<List<SalesData>> GetSalesDataAsync(DateTime startDate, DateTime endDate, string filter = null)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportToPdfAsync(DashboardMetrics metrics)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportToExcelAsync(List<SalesData> data)
        {
            throw new NotImplementedException();
        }

        public Task<List<ChartDataPoint>> GetTrendDataAsync(string metric, int periods)
        {
            throw new NotImplementedException();
        }
    }
}
