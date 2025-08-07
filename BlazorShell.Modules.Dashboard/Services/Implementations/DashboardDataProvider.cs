using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorShell.Modules.Dashboard.Models;
using BlazorShell.Modules.Dashboard.Services.Interfaces;

namespace BlazorShell.Modules.Dashboard.Services.Implementations;

public class DashboardDataProvider : IDashboardDataProvider
{
    public async Task<object?> GetDataAsync(string dataType)
    {
        await Task.Delay(100);

        return dataType switch
        {
            "stats" => new { Users = Random.Shared.Next(100, 1000), Sessions = Random.Shared.Next(50, 500), Revenue = Random.Shared.Next(10000, 100000) },
            "chart" => Enumerable.Range(1, 7).Select(i => Random.Shared.Next(10, 100)).ToArray(),
            "activities" => new[] { "Activity 1", "Activity 2", "Activity 3" },
            _ => null
        };
    }

    public async Task<ChartData> GetChartDataAsync(string chartType)
    {
        await Task.Delay(100);

        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var data = Enumerable.Range(0, 7).Select(_ => Random.Shared.Next(20, 100)).ToArray();

        return new ChartData
        {
            Labels = labels,
            Datasets = new[]
            {
                new ChartDataset
                {
                    Label = "This Week",
                    Data = data,
                    BackgroundColor = "rgba(54, 162, 235, 0.2)",
                    BorderColor = "rgba(54, 162, 235, 1)"
                }
            }
        };
    }
}
