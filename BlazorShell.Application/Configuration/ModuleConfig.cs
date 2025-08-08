using System.Collections.Generic;

namespace BlazorShell.Application.Configuration;

public class ModuleConfig
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? AssemblyName { get; set; }
    public string? EntryType { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public bool Enabled { get; set; }
    public int LoadOrder { get; set; }
    public List<string>? Dependencies { get; set; }
    public string? RequiredRole { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    public List<NavigationItemConfig>? NavigationItems { get; set; }
    public List<PermissionConfig>? Permissions { get; set; }
}
