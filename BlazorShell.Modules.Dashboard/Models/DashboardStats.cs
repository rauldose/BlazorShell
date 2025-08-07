using System;

namespace BlazorShell.Modules.Dashboard.Models;

public class DashboardStats
{
    public int TotalVisits { get; set; }
    public int UniqueVisitors { get; set; }
    public int PageViews { get; set; }
    public double BounceRate { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
}
