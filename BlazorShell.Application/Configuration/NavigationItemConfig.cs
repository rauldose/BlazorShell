using System.Collections.Generic;

namespace BlazorShell.Application.Configuration;

public class NavigationItemConfig
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public int Order { get; set; }
    public string? Type { get; set; }
    public string? RequiredPermission { get; set; }
    public string? Parent { get; set; }
    public List<NavigationItemConfig>? Children { get; set; }
}
