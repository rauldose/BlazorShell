namespace BlazorShell.Modules.Admin.Services.Models;

public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsLoaded { get; set; }
    public bool IsCore { get; set; }
    public int LoadOrder { get; set; }
    public string AssemblyPath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public DateTime? LoadedAt { get; set; }
    public long FileSize { get; set; }
    public int ComponentCount { get; set; }
    public int NavigationItemCount { get; set; }
    public ModuleStatus Status { get; set; }
}
