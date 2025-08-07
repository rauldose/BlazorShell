using System.Collections.Generic;

namespace BlazorShell.Modules.Dashboard.Models;

public class Widget
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}
