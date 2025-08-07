using System;

namespace BlazorShell.Modules.Dashboard.Models;

public class ChartData
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public ChartDataset[] Datasets { get; set; } = Array.Empty<ChartDataset>();
}
