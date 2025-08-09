using BlazorShell.Modules.Analytics.Domain.Entities;
using BlazorShell.Modules.Analytics.Domain.Interfaces;
using BlazorShell.Modules.Analytics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _context;
    private readonly ILogger<AnalyticsRepository> _logger;

    public AnalyticsRepository(AnalyticsDbContext context, ILogger<AnalyticsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Basic CRUD operations
    public async Task<SalesData> GetByIdAsync(int id)
    {
        return await _context.Set<SalesData>().FindAsync(id);
    }

    public async Task<List<SalesData>> GetAllAsync()
    {
        return await _context.Set<SalesData>()
            .OrderByDescending(s => s.Date)
            .ToListAsync();
    }

    public async Task<SalesData> AddAsync(SalesData salesData)
    {
        _context.Set<SalesData>().Add(salesData);
        await _context.SaveChangesAsync();
        return salesData;
    }

    public async Task<SalesData> UpdateAsync(SalesData salesData)
    {
        _context.Entry(salesData).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return salesData;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _context.Set<SalesData>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    // Bulk operations
    public async Task BulkInsertAsync(List<SalesData> salesDataList)
    {
        if (salesDataList == null || !salesDataList.Any())
            return;

        try
        {
            _logger.LogInformation($"Bulk inserting {salesDataList.Count} sales records");

            // For better performance with large datasets
            await _context.Set<SalesData>().AddRangeAsync(salesDataList);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully inserted {salesDataList.Count} records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk insert");
            throw;
        }
    }

    public async Task<int> BulkUpdateAsync(List<SalesData> salesDataList)
    {
        if (salesDataList == null || !salesDataList.Any())
            return 0;

        _context.Set<SalesData>().UpdateRange(salesDataList);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> BulkDeleteAsync(List<int> ids)
    {
        if (ids == null || !ids.Any())
            return 0;

        var entities = await _context.Set<SalesData>()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        _context.Set<SalesData>().RemoveRange(entities);
        return await _context.SaveChangesAsync();
    }

    // Query operations
    public async Task<List<SalesData>> GetSalesDataRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }

    public async Task<List<SalesData>> GetByRegionAsync(string region, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().Where(s => s.Region == region);

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.OrderByDescending(s => s.Date).ToListAsync();
    }

    public async Task<List<SalesData>> GetByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().Where(s => s.ProductCategory == category);

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.OrderByDescending(s => s.Date).ToListAsync();
    }

    public async Task<List<SalesData>> GetBySalesRepAsync(string salesRep, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().Where(s => s.SalesRepresentative == salesRep);

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.OrderByDescending(s => s.Date).ToListAsync();
    }

    public async Task<List<SalesData>> GetByChannelAsync(string channel, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().Where(s => s.Channel == channel);

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.OrderByDescending(s => s.Date).ToListAsync();
    }

    // Aggregation operations
    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.SumAsync(s => s.Revenue);
    }

    public async Task<decimal> GetTotalProfitAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        return await query.SumAsync(s => s.Revenue - s.Cost);
    }

    public async Task<Dictionary<string, decimal>> GetRevenueByRegionAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Region)
            .Select(g => new { Region = g.Key, Revenue = g.Sum(s => s.Revenue) })
            .ToDictionaryAsync(x => x.Region, x => x.Revenue);

        return result;
    }

    public async Task<Dictionary<string, decimal>> GetRevenueByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.ProductCategory)
            .Select(g => new { Category = g.Key, Revenue = g.Sum(s => s.Revenue) })
            .ToDictionaryAsync(x => x.Category, x => x.Revenue);

        return result;
    }

    public async Task<Dictionary<string, int>> GetOrderCountByChannelAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Channel)
            .Select(g => new { Channel = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Channel, x => x.Count);

        return result;
    }

    // Analytics specific
    public async Task<List<TopPerformer>> GetTopProductsAsync(int count, DateTime startDate, DateTime endDate)
    {
        var topProducts = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.ProductCategory)
            .Select(g => new TopPerformer
            {
                Name = g.Key,
                Value = g.Sum(s => s.Revenue),
                Rank = 0,
                ChangePercent = 0 // Will be calculated in service layer
            })
            .OrderByDescending(t => t.Value)
            .Take(count)
            .ToListAsync();

        // Assign ranks
        for (int i = 0; i < topProducts.Count; i++)
        {
            topProducts[i].Rank = i + 1;
        }

        return topProducts;
    }

    public async Task<List<TopPerformer>> GetTopSalesRepsAsync(int count, DateTime startDate, DateTime endDate)
    {
        var topReps = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.SalesRepresentative)
            .Select(g => new TopPerformer
            {
                Name = g.Key,
                Value = g.Sum(s => s.Revenue),
                Rank = 0,
                ChangePercent = 0 // Will be calculated in service layer
            })
            .OrderByDescending(t => t.Value)
            .Take(count)
            .ToListAsync();

        // Assign ranks
        for (int i = 0; i < topReps.Count; i++)
        {
            topReps[i].Rank = i + 1;
        }

        return topReps;
    }

    public async Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(s => s.Revenue) })
            .OrderBy(x => x.Date)
            .ToDictionaryAsync(x => x.Date, x => x.Revenue);

        return result;
    }

    public async Task<Dictionary<DateTime, decimal>> GetMonthlyRevenueAsync(int months)
    {
        var startDate = DateTime.Now.AddMonths(-months);

        var result = await _context.Set<SalesData>()
            .Where(s => s.Date >= startDate)
            .GroupBy(s => new { s.Date.Year, s.Date.Month })
            .Select(g => new
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Revenue = g.Sum(s => s.Revenue)
            })
            .OrderBy(x => x.Date)
            .ToDictionaryAsync(x => x.Date, x => x.Revenue);

        return result;
    }

    // Performance metrics
    public async Task<double> GetAverageOrderValueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        if (!await query.AnyAsync())
            return 0;

        return await query.AverageAsync(s => (double)s.Revenue);
    }

    public async Task<double> GetConversionRateAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // This is a simplified implementation
        // In a real scenario, you'd need visitor/session data
        var query = _context.Set<SalesData>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        var orderCount = await query.CountAsync();

        // Simulated conversion rate calculation
        // In production, this would be: (orders / visitors) * 100
        return orderCount > 0 ? 3.5 : 0; // Placeholder value
    }

    public async Task<int> GetCustomerCountAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<SalesData>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);

        // Count distinct customer segments as a proxy for customer count
        // In production, you'd have actual customer IDs
        return await query.Select(s => s.CustomerSegment).Distinct().CountAsync();
    }

    // Utility methods
    public async Task<bool> HasDataAsync()
    {
        return await _context.Set<SalesData>().AnyAsync();
    }

    public async Task<DateTime?> GetEarliestDateAsync()
    {
        return await _context.Set<SalesData>()
            .MinAsync(s => (DateTime?)s.Date);
    }

    public async Task<DateTime?> GetLatestDateAsync()
    {
        return await _context.Set<SalesData>()
            .MaxAsync(s => (DateTime?)s.Date);
    }

    public async Task<int> GetRecordCountAsync()
    {
        return await _context.Set<SalesData>().CountAsync();
    }
}