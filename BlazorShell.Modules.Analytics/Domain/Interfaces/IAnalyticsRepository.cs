using BlazorShell.Modules.Analytics.Domain.Entities;
using BlazorShell.Modules.Analytics.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Domain.Interfaces
{
    public interface IAnalyticsRepository
    {
        // Basic CRUD operations
        Task<SalesData> GetByIdAsync(int id);
        Task<List<SalesData>> GetAllAsync();
        Task<SalesData> AddAsync(SalesData salesData);
        Task<SalesData> UpdateAsync(SalesData salesData);
        Task<bool> DeleteAsync(int id);

        // Bulk operations
        Task BulkInsertAsync(List<SalesData> salesDataList);
        Task<int> BulkUpdateAsync(List<SalesData> salesDataList);
        Task<int> BulkDeleteAsync(List<int> ids);

        // Query operations
        Task<List<SalesData>> GetSalesDataRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<SalesData>> GetByRegionAsync(string region, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<SalesData>> GetByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<SalesData>> GetBySalesRepAsync(string salesRep, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<SalesData>> GetByChannelAsync(string channel, DateTime? startDate = null, DateTime? endDate = null);

        // Aggregation operations
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalProfitAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> GetRevenueByRegionAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetRevenueByCategoryAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetOrderCountByChannelAsync(DateTime startDate, DateTime endDate);

        // Analytics specific
        Task<List<TopPerformer>> GetTopProductsAsync(int count, DateTime startDate, DateTime endDate);
        Task<List<TopPerformer>> GetTopSalesRepsAsync(int count, DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, decimal>> GetMonthlyRevenueAsync(int months);

        // Performance metrics
        Task<double> GetAverageOrderValueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<double> GetConversionRateAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetCustomerCountAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Utility
        Task<bool> HasDataAsync();
        Task<DateTime?> GetEarliestDateAsync();
        Task<DateTime?> GetLatestDateAsync();
        Task<int> GetRecordCountAsync();
    }
}
