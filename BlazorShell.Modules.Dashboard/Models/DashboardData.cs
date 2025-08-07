using System;
using System.Collections.Generic;

namespace BlazorShell.Modules.Dashboard.Models;

public class DashboardData
{
    public int TotalUsers { get; set; }
    public int ActiveSessions { get; set; }
    public decimal TotalRevenue { get; set; }
    public double GrowthRate { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> RecentActivities { get; set; } = new();
}
