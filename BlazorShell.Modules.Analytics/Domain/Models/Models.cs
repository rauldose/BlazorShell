using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Domain.Models
{
    public class DashboardMetrics
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal GrowthPercentage { get; set; }
        public List<ChartDataPoint> RevenueByMonth { get; set; }
        public List<ChartDataPoint> RevenueByCategory { get; set; }
        public List<ChartDataPoint> RevenueByRegion { get; set; }
        public List<TopPerformer> TopProducts { get; set; }
        public List<TopPerformer> TopSalesReps { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string Color { get; set; }
    }

    public class TopPerformer
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int Rank { get; set; }
        public decimal ChangePercent { get; set; }
    }
}
