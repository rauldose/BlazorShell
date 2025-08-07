using System;

namespace BlazorShell.Modules.Dashboard.Models;

public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public int[] Data { get; set; } = Array.Empty<int>();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
}
